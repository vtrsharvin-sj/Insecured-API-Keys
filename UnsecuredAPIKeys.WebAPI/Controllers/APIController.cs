using Humanizer;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

using UnsecuredAPIKeys.Data;
using UnsecuredAPIKeys.Data.Common;
using UnsecuredAPIKeys.Data.Models;
using UnsecuredAPIKeys.WebAPI.Commons;
using UnsecuredAPIKeys.WebAPI.Hubs;
using UnsecuredAPIKeys.WebAPI.Services;

namespace UnsecuredAPIKeys.WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class APIController(
        ILogger<APIController> logger,
        DBContext dbContext,
        IMemoryCache memoryCache,
        IDisplayCountService displayCountService,
        IActiveUserService activeUserService,
        IHubContext<StatsHub> hubContext)
        : ControllerBase
    {
        private const string ApiKeyListCacheKey = "ApiKeys_ValidList";
        private static readonly TimeSpan ApiKeyListCacheExpiration = TimeSpan.FromMinutes(5);

        private const string ApiKeyStatsCacheKey = "ApiKeys_Statistics";
        private static readonly TimeSpan ApiKeyStatsCacheExpiration = TimeSpan.FromSeconds(30);

        private const string InvalidationStatsCacheKey = "Invalidation_Statistics";
        private static readonly TimeSpan InvalidationStatsCacheExpiration = TimeSpan.FromMinutes(5);

        [HttpGet("GetRandomKey")]
        [AllowedReferrers]
        [RateLimit]
        public async Task<ActionResult<APIKey?>> GetRandomKey([FromQuery] int? type = null)
        {
            var key = await GetRandomApiKeyAndUpdateDisplayCount(type);
            return Ok(key);
        }

        [HttpGet("GetDisplayCount")]
        public ActionResult<long> GetDisplayCount()
        {
            return Ok(displayCountService.TotalDisplayCount);
        }

        [HttpGet("GetActiveUserCount")]
        public ActionResult<int> GetActiveUserCount()
        {
            return Ok(activeUserService.ActiveUserCount);
        }

        [HttpPost("DebugActiveUsers")]
        public async Task<ActionResult<object>> DebugActiveUsers()
        {
            // Only allow in Development to avoid exposing internal state in production
            if (!HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
                return NotFound();

            await activeUserService.ValidateConnectionsAsync();
            return Ok(new {
                ActiveUserCount = activeUserService.ActiveUserCount,
                Message = "Validation triggered. Check logs for details."
            });
        }

        [HttpGet("GetRateLimitInfo")]
        public async Task<ActionResult<object>> GetRateLimitInfo()
        {
            // Get the rate limit from ApplicationSettings
            var rateLimitSetting = await dbContext.ApplicationSettings
                .FirstOrDefaultAsync(s => s.Key == "RateLimit");
            
            var maxRequests = 5; // Default fallback
            if (rateLimitSetting != null && int.TryParse(rateLimitSetting.Value, out var parsedLimit))
            {
                maxRequests = parsedLimit;
            }
            
            // Return the current rate limiting configuration
            return Ok(new
            {
                MaxRequests = maxRequests,
                TimeWindowHours = 1,
                Description = "Fair usage limits to ensure service availability for all users",
                Note = "After reaching the limit, a fallback key will be provided instead of blocking access"
            });
        }

        [HttpGet("GetKeyStatistics")]
        public async Task<ActionResult<object>> GetKeyStatistics()
        {
            if (memoryCache.TryGetValue(ApiKeyStatsCacheKey, out object? cachedStats))
            {
                logger.LogInformation("Returning statistics from cache.");
                return cachedStats!;
            }

            logger.LogInformation("Calculating statistics from database.");
            
            // Optimized single query to get all statistics
            var startOfTodayUtc = DateTime.UtcNow.Date;
            var statsQuery = await dbContext.APIKeys
                .GroupBy(x => 1) // Group all into single row
                .Select(g => new
                {
                    TotalNumberOfKeys = g.Count(),
                    NumberOfValidKeys = g.Count(x => x.Status == ApiStatusEnum.Valid),
                    NewKeysFoundToday = g.Count(x => x.FirstFoundUTC >= startOfTodayUtc),
                    TotalFixedKeys = g.Count(x => x.Status == ApiStatusEnum.NoLongerWorking),
                    TotalInvalidatedKeys = g.Count(x => x.Status == ApiStatusEnum.Invalid),
                    ValidNoCreditsKeys = g.Count(x => x.Status == ApiStatusEnum.ValidNoCredits)
                })
                .FirstOrDefaultAsync();

            var mostRecentKey = await dbContext.APIKeys
                .OrderByDescending(x => x.FirstFoundUTC)
                .Select(x => x.FirstFoundUTC)
                .FirstOrDefaultAsync();
            
            var timeAgo = mostRecentKey == default ? null : mostRecentKey.Humanize();

            // Get invalidation count
            var totalInvalidated = await dbContext.KeyInvalidations.CountAsync();

            var stats = new
            {
                TotalNumberOfKeys = statsQuery?.TotalNumberOfKeys ?? 0,
                NumberOfValidKeys = statsQuery?.NumberOfValidKeys ?? 0,
                NewKeysFoundToday = statsQuery?.NewKeysFoundToday ?? 0,
                MostRecentFind = timeAgo,
                TotalFixedKeys = statsQuery?.TotalFixedKeys ?? 0,
                TotalInvalidatedKeys = totalInvalidated,
                ValidNoCreditsKeys = statsQuery?.ValidNoCreditsKeys ?? 0
            };

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(ApiKeyStatsCacheExpiration)
                .SetPriority(CacheItemPriority.Normal);

            memoryCache.Set(ApiKeyStatsCacheKey, stats, cacheOptions);

            return Ok(stats);
        }

        [HttpGet("GetInvalidationStats")]
        public async Task<ActionResult<object>> GetInvalidationStats()
        {
            if (memoryCache.TryGetValue(InvalidationStatsCacheKey, out object? cachedStats))
            {
                logger.LogInformation("Returning invalidation statistics from cache.");
                return cachedStats!;
            }

            var startOfTodayUtc = DateTime.UtcNow.Date;
            var stats = new
            {
                TotalInvalidated = await dbContext.KeyInvalidations.CountAsync(),
                InvalidatedToday = await dbContext.KeyInvalidations
                    .CountAsync(x => x.InvalidatedAt >= startOfTodayUtc),
                AverageLifespanDays = await dbContext.KeyInvalidations
                    .AverageAsync(x => (double?)x.DaysActive) ?? 0,
                ConfirmedFixed = await dbContext.KeyInvalidations
                    .CountAsync(x => x.ConfirmedFixed),
                TopInvalidationReasons = await dbContext.KeyInvalidations
                    .GroupBy(x => x.InvalidationReason ?? "Unknown")
                    .Select(g => new { Reason = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(5)
                    .ToListAsync(),
                InvalidationsByType = await dbContext.KeyInvalidations
                    .Include(k => k.ApiKey)
                    .GroupBy(x => x.ApiKey.ApiType)
                    .Select(g => new { ApiType = g.Key.ToString(), Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToListAsync(),
                RecentInvalidations = await dbContext.KeyInvalidations
                    .OrderByDescending(x => x.InvalidatedAt)
                    .Take(10)
                    .Select(x => new
                    {
                        x.InvalidatedAt,
                        x.DaysActive,
                        x.InvalidationReason,
                        ApiType = x.ApiKey.ApiType.ToString()
                    })
                    .ToListAsync()
            };

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(InvalidationStatsCacheExpiration)
                .SetPriority(CacheItemPriority.Normal);

            memoryCache.Set(InvalidationStatsCacheKey, stats, cacheOptions);

            return Ok(stats);
        }

        [HttpGet("GetPatternEffectiveness")]
        public async Task<ActionResult<List<PatternEffectivenessDto>>> GetPatternEffectiveness()
        {
            // First, get the data from the database without calculating SuccessRate
            var patternsData = await dbContext.PatternEffectiveness
                .OrderByDescending(p => p.ValidKeys)
                .ThenByDescending(p => p.TotalMatches)
                .Take(50)
                .Select(p => new 
                {
                    p.Pattern,
                    p.ProviderName,
                    p.TotalMatches,
                    p.ValidKeys,
                    p.InvalidKeys,
                    p.LastUpdated
                })
                .ToListAsync();

            // Then calculate SuccessRate in memory to avoid division by zero in SQL
            var patterns = patternsData.Select(p => new PatternEffectivenessDto
            {
                Pattern = p.Pattern,
                ProviderName = p.ProviderName,
                TotalMatches = p.TotalMatches,
                ValidKeys = p.ValidKeys,
                InvalidKeys = p.InvalidKeys,
                SuccessRate = p.TotalMatches > 0 ? Math.Round((double)p.ValidKeys / p.TotalMatches * 100, 2) : 0,
                LastUpdated = p.LastUpdated
            }).ToList();

            return Ok(patterns);
        }

        [HttpGet("GetKeyRotations")]
        public async Task<ActionResult<List<KeyRotationDto>>> GetKeyRotations([FromQuery] int count = 20)
        {
            var rotations = await dbContext.KeyRotations
                .OrderByDescending(r => r.RotatedAt)
                .Take(Math.Min(count, 100))
                .Select(r => new KeyRotationDto
                {
                    RotatedAt = r.RotatedAt,
                    RepoUrl = r.RepoUrl,
                    OldKeyDaysActive = r.OldKeyDaysActive,
                    OldKeyType = r.OldKey.ApiType.ToString(),
                    NewKeyType = r.NewKey.ApiType.ToString()
                })
                .ToListAsync();

            return Ok(rotations);
        }

        [HttpPost("webhook/key-fixed")]
        public async Task<IActionResult> KeyFixedWebhook([FromBody] KeyFixedNotification notification)
        {
            try
            {
                var key = await dbContext.APIKeys.FindAsync(notification.KeyId);
                if (key != null)
                {
                    var previousStatus = key.Status;
                    key.Status = ApiStatusEnum.NoLongerWorking;
                    key.LastCheckedUTC = DateTime.UtcNow;
                    
                    // Track the invalidation
                    var invalidation = new KeyInvalidation
                    {
                        ApiKeyId = key.Id,
                        InvalidatedAt = DateTime.UtcNow,
                        ConfirmedFixed = true,
                        FixedAt = DateTime.UtcNow,
                        InvalidationReason = notification.Reason ?? "Fixed by repository owner",
                        WasValid = previousStatus == ApiStatusEnum.Valid,
                        DaysActive = (DateTime.UtcNow - key.FirstFoundUTC).Days,
                        PreviousStatus = previousStatus.ToString()
                    };
                    
                    dbContext.KeyInvalidations.Add(invalidation);
                    await dbContext.SaveChangesAsync();
                    
                    logger.LogInformation("Key {KeyId} marked as fixed via webhook. Reason: {Reason}", 
                        key.Id, notification.Reason);
                    
                    // Clear cache
                    memoryCache.Remove(ApiKeyStatsCacheKey);
                    memoryCache.Remove(InvalidationStatsCacheKey);
                    
                    return Ok(new { success = true, message = "Key marked as fixed" });
                }
                
                return NotFound(new { success = false, message = "Key not found" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing key-fixed webhook");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpGet("GetKeyTypes")]
        public async Task<ActionResult<List<ApiTypeCountDto>>> GetKeyTypes()
        {
            var typeCounts = await dbContext.APIKeys
                .Where(x => x.Status != ApiStatusEnum.Removed)
                .GroupBy(x => x.ApiType)
                .Select(g => new ApiTypeCountDto
                {
                    ApiType = g.Key.ToString(),
                    KeyCount = g.Count(),
                    ApiTypeId = (int)g.Key
                })
                .ToListAsync();

            return Ok(typeCounts);
        }

        [HttpPost("TrackIssueSubmission")]
        public async Task<IActionResult> TrackIssueSubmission([FromBody] IssueSubmissionTrackingDto request)
        {
            try
            {
                var userIP = GetClientIpAddress(HttpContext);

                // Check if this user has already submitted an issue for this API key
                var existingTracking = await dbContext.IssueSubmissionTrackings
                    .FirstOrDefaultAsync(x =>
                        x.ApiKeyId == request.ApiKeyId &&
                        x.UserIP == userIP &&
                        x.RepoUrl == request.RepoUrl);

                if (existingTracking != null)
                {
                    // Update the existing entry with the latest submission time
                    existingTracking.SubmittedAt = DateTime.UtcNow;
                    logger.LogInformation("Updated existing issue submission tracking for ApiKey {ApiKeyId} from IP {UserIP}",
                        request.ApiKeyId, userIP);
                }
                else
                {
                    // Create new tracking entry
                    var tracking = new IssueSubmissionTracking
                    {
                        ApiKeyId = request.ApiKeyId,
                        ApiType = request.ApiType,
                        RepoUrl = request.RepoUrl,
                        SubmittedAt = DateTime.UtcNow,
                        UserIP = userIP
                    };

                    dbContext.IssueSubmissionTrackings.Add(tracking);
                    logger.LogInformation("Created new issue submission tracking for ApiKey {ApiKeyId} from IP {UserIP}",
                        request.ApiKeyId, userIP);
                }

                await dbContext.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to track issue submission");
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Accepts a user-contributed GitHub PAT, validates it against the GitHub API,
        /// then stores it in SearchProviderTokens for the scraper to use.
        /// </summary>
        [HttpPost("SubmitToken")]
        public async Task<IActionResult> SubmitToken([FromBody] SubmitTokenDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Token))
                return BadRequest(new { error = "Token cannot be empty." });

            // Reject obviously non-PAT strings
            if (request.Token.Length < 20)
                return BadRequest(new { error = "That doesn't look like a valid GitHub token." });

            // Validate the token against GitHub before saving
            try
            {
                using var http = new System.Net.Http.HttpClient();
                http.DefaultRequestHeaders.Add("User-Agent", "InsecuredAPIKeys-TokenValidator/1.0");
                http.DefaultRequestHeaders.Add("Authorization", $"token {request.Token}");
                http.Timeout = TimeSpan.FromSeconds(10);

                var ghResponse = await http.GetAsync("https://api.github.com/user");
                if (!ghResponse.IsSuccessStatusCode)
                    return BadRequest(new { error = "GitHub rejected that token. Make sure it has the public_repo scope." });
            }
            catch (Exception)
            {
                return StatusCode(503, new { error = "Could not reach GitHub to validate the token. Please try again." });
            }

            // Check if this token is already in the database
            var exists = await dbContext.SearchProviderTokens
                .AnyAsync(t => t.Token == request.Token);

            if (exists)
                return Conflict(new { error = "This token has already been submitted." });

            dbContext.SearchProviderTokens.Add(new SearchProviderToken
            {
                Token       = request.Token,
                SearchProvider = SearchProviderEnum.GitHub,
                IsEnabled   = true,
                LastUsedUTC = null
            });

            await dbContext.SaveChangesAsync();
            logger.LogInformation("New GitHub token contributed by user (IP: {IP})", GetClientIpAddress(HttpContext));

            return Ok(new { message = "Token accepted — thank you! The scraper will start using it shortly." });
        }

        [HttpGet("GetSnitchLeaderboard")]
        public async Task<ActionResult<List<SnitchLeaderboardDto>>> GetSnitchLeaderboard([FromQuery] int count = 10)
        {
            try
            {
                var topSnitches = await dbContext.SnitchLeaderboards
                    .OrderByDescending(s => s.SnitchScore)
                    .Take(Math.Min(count, 50)) // Cap at 50 for performance
                    .ToListAsync();

                var leaderboard = topSnitches.Select((entry, index) => new SnitchLeaderboardDto
                {
                    Rank = index + 1,
                    DisplayName = entry.DisplayName ?? GenerateAnonymousDisplayName(entry.UserIdentifier),
                    TotalIssuesSubmitted = entry.TotalIssuesSubmitted,
                    OpenIssuesSubmitted = entry.OpenIssuesSubmitted,
                    ClosedIssuesSubmitted = entry.ClosedIssuesSubmitted,
                    TotalRepositoriesAffected = entry.TotalRepositoriesAffected,
                    FavoriteApiType = entry.FavoriteApiType,
                    SnitchScore = entry.SnitchScore,
                    FirstSubmissionAt = entry.FirstSubmissionAt,
                    LastSubmissionAt = entry.LastSubmissionAt,
                    ConsecutiveDaysActive = entry.ConsecutiveDaysActive
                }).ToList();

                return Ok(leaderboard);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to retrieve snitch leaderboard");
                return StatusCode(500);
            }
        }

        [HttpGet("GetLeaderboardStats")]
        public async Task<ActionResult<object>> GetLeaderboardStats()
        {
            try
            {
                var totalSnitches = await dbContext.SnitchLeaderboards.CountAsync();
                var totalIssuesSubmitted = await dbContext.IssueSubmissionTrackings.CountAsync();
                var totalIssuesVerified = await dbContext.IssueVerifications.CountAsync();
                var openIssues = await dbContext.IssueVerifications.CountAsync(iv => iv.Status == IssueVerificationStatus.Open);
                var closedIssues = await dbContext.IssueVerifications.CountAsync(iv => iv.Status == IssueVerificationStatus.Closed);

                var stats = new
                {
                    TotalSnitches = totalSnitches,
                    TotalIssuesSubmitted = totalIssuesSubmitted,
                    TotalIssuesVerified = totalIssuesVerified,
                    OpenIssues = openIssues,
                    ClosedIssues = closedIssues,
                    SuccessRate = totalIssuesVerified > 0 ? Math.Round((double)(openIssues + closedIssues) / totalIssuesVerified * 100, 1) : 0
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to retrieve leaderboard statistics");
                return StatusCode(500);
            }
        }

        private static string GenerateAnonymousDisplayName(string userIdentifier)
        {
            var hash = userIdentifier.GetHashCode();
            var adjectives = new[] { "Anonymous", "Mysterious", "Stealthy", "Vigilant", "Alert", "Sharp", "Keen", "Clever", "Swift", "Silent" };
            var nouns = new[] { "Snitch", "Watcher", "Guardian", "Detective", "Hunter", "Scout", "Finder", "Seeker", "Tracker", "Spy" };
            
            var adjIndex = Math.Abs(hash) % adjectives.Length;
            var nounIndex = Math.Abs(hash / adjectives.Length) % nouns.Length;
            
            return $"{adjectives[adjIndex]} {nouns[nounIndex]} #{Math.Abs(hash) % 1000:D3}";
        }

        private async Task<APIKey?> GetRandomApiKeyAndUpdateDisplayCount(int? apiType = null)
        {
            // For random selection, we'll use a more efficient approach
            // Get the count of valid keys first
            // Showing only valid keys for the last 24 hours
            var query = dbContext.APIKeys
                .Where(s => s.Status == ApiStatusEnum.Valid &&
                            s.LastCheckedUTC > DateTime.UtcNow.AddHours(-24));

            if (apiType.HasValue)
            {
                query = query.Where(k => (int)k.ApiType == apiType.Value);
            }

            var totalCount = await query.CountAsync();
            
            if (totalCount == 0)
            {
                logger.LogWarning("No valid API keys found in the database{S}.", apiType.HasValue ? $" for type {apiType}" : "");
                return null;
            }

            // Select a random index
            var random = new Random();
            var randomIndex = random.Next(totalCount);

            // Get the key at that index using Skip
            var selectedKey = await query
                .OrderBy(x => x.Id) // Ensure consistent ordering
                .Skip(randomIndex)
                .Take(1)
                .Include(x => x.References)
                .FirstOrDefaultAsync();

            if (selectedKey != null)
            {
                // Update the display count
                selectedKey.TimesDisplayed++;
                await dbContext.SaveChangesAsync();
                
                // Update the in-memory count and broadcast
                displayCountService.IncrementCount();
                await hubContext.Clients.All.SendAsync("DisplayCountUpdated", displayCountService.TotalDisplayCount);
                
                logger.LogInformation("Incremented display count for Key ID: {Id}, Total: {TotalCount}", 
                    selectedKey.Id, displayCountService.TotalDisplayCount);
                return selectedKey;
            }

            logger.LogError("Could not retrieve random key from database.");
            return null;
        }

        private string GetClientIpAddress(HttpContext context)
        {
            // Try to get from X-Forwarded-For header first (for clients behind proxies)
            string? ip = context.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();

            // If not available, use the remote IP address
            if (string.IsNullOrEmpty(ip))
            {
                ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            }

            return ip;
        }

        [HttpGet("GetAllValidKeys")]
        [AllowedReferrers]
        [RateLimit(500)]
        public async Task<ActionResult<PaginatedResponse<APIKey>>> GetAllValidKeys(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] int? type = null,
            [FromQuery] string? statusFilter = null)
        {
            // Validate pagination parameters
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;
            if (pageSize > 100) pageSize = 100; // Cap at 100 for performance

            // Build query — show all keys by default; "valid" filter shows only Valid status
            var query = statusFilter == "valid"
                ? dbContext.APIKeys.Include(x => x.References).Where(s => s.Status == ApiStatusEnum.Valid)
                : dbContext.APIKeys.Include(x => x.References).Where(s => s.Status != ApiStatusEnum.Removed);

            // Add type filter if specified
            if (type.HasValue)
            {
                query = query.Where(k => (int)k.ApiType == type.Value);
            }

            // Get total count for pagination metadata
            var totalCount = await query.CountAsync();

            // Apply pagination — newest first so fresh finds appear at the top
            var items = await query
                .OrderByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var response = new PaginatedResponse<APIKey>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };

            return Ok(response);
        }

        // DTOs
        public class SnitchLeaderboardDto
        {
            public int Rank { get; set; }
            public string DisplayName { get; set; } = string.Empty;
            public int TotalIssuesSubmitted { get; set; }
            public int OpenIssuesSubmitted { get; set; }
            public int ClosedIssuesSubmitted { get; set; }
            public int TotalRepositoriesAffected { get; set; }
            public string? FavoriteApiType { get; set; }
            public double SnitchScore { get; set; }
            public DateTime FirstSubmissionAt { get; set; }
            public DateTime LastSubmissionAt { get; set; }
            public int ConsecutiveDaysActive { get; set; }
        }

        public class IssueSubmissionTrackingDto
        {
            public int ApiKeyId { get; set; }
            public string ApiType { get; set; } = string.Empty;
            public string RepoUrl { get; set; } = string.Empty;
            public string Timestamp { get; set; } = string.Empty;
        }

        public class PaginatedResponse<T>
        {
            public List<T> Items { get; set; } = new();
            public int Page { get; set; }
            public int PageSize { get; set; }
            public int TotalCount { get; set; }
            public int TotalPages { get; set; }
        }

        public class ApiTypeCountDto
        {
            public string ApiType { get; set; } = string.Empty;
            public int KeyCount { get; set; }
            public int ApiTypeId { get; set; }  // This will hold the enum value
        }

        public class PatternEffectivenessDto
        {
            public string Pattern { get; set; } = string.Empty;
            public string ProviderName { get; set; } = string.Empty;
            public int TotalMatches { get; set; }
            public int ValidKeys { get; set; }
            public int InvalidKeys { get; set; }
            public double SuccessRate { get; set; }
            public DateTime LastUpdated { get; set; }
        }

        public class KeyRotationDto
        {
            public DateTime RotatedAt { get; set; }
            public string RepoUrl { get; set; } = string.Empty;
            public int OldKeyDaysActive { get; set; }
            public string OldKeyType { get; set; } = string.Empty;
            public string NewKeyType { get; set; } = string.Empty;
        }

        public class KeyFixedNotification
        {
            public long KeyId { get; set; }
            public string? Reason { get; set; }
            public string? ReporterEmail { get; set; }
        }

        public class SubmitTokenDto
        {
            public string Token { get; set; } = string.Empty;
        }
    }
}
