using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using UnsecuredAPIKeys.Data;

namespace UnsecuredAPIKeys.WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StatusController(
        ILogger<StatusController> logger,
        DBContext dbContext,
        ISystemdService systemdService)
        : ControllerBase
    {
        [HttpGet("GetStatus")]
        public async Task<IActionResult> GetStatus([FromQuery] bool includeLogs = false, [FromQuery] int logLines = 50)
        {
            try
            {
                var dbStatus = await dbContext.Database.CanConnectAsync();
                var servicesStatus = await systemdService.GetServicesStatusAsync(includeLogs, logLines);

                var status = new
                {
                    ApiStatus = "UP",
                    DatabaseConnection = dbStatus,
                    Services = servicesStatus
                };

                return Ok(status);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting status");
                return StatusCode(500, new { error = "Error getting status", details = ex.Message });
            }
        }

        public class LogEntry
        {
            // Standard journalctl JSON fields
            public string? __REALTIME_TIMESTAMP { get; set; }
            public string? PRIORITY { get; set; }
            public string? MESSAGE { get; set; }
            public string? SYSLOG_IDENTIFIER { get; set; }
            public string? _SYSTEMD_UNIT { get; set; }

            // Helper properties for display
            public string FormattedTimestamp
            {
                get
                {
                    if (string.IsNullOrEmpty(__REALTIME_TIMESTAMP)) return string.Empty;

                    // Convert microseconds since epoch to DateTime
                    if (long.TryParse(__REALTIME_TIMESTAMP, out long timestamp))
                    {
                        try
                        {
                            // journalctl timestamps are in microseconds
                            var dateTime = DateTimeOffset.FromUnixTimeMilliseconds(timestamp / 1000)
                                .DateTime.ToUniversalTime();
                            return dateTime.ToString("yyyy-MM-dd HH:mm:ss");
                        }
                        catch
                        {
                            return __REALTIME_TIMESTAMP;
                        }
                    }
                    return __REALTIME_TIMESTAMP;
                }
            }
        }

        public class ServiceStatus
        {
            public string ServiceName { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string LastRunTime { get; set; } = string.Empty;
            public bool IsActive { get; set; }
            public string Description { get; set; } = string.Empty;
            public List<LogEntry> RecentLogs { get; set; } = [];
            public string LogError { get; set; } = string.Empty;

            public int? NumberOfInstances { get; set; } // Cloud Instances?

            public string MonitoringStatus => $"{ServiceName} IsActive: {IsActive}";
        }

        public class SystemServicesStatus
        {
            public ServiceStatus ScraperService { get; set; } = new();
            public ServiceStatus VerifierService { get; set; } = new();
            public DateTime StatusCheckedAt { get; set; } = DateTime.UtcNow;

            public bool AllServicesRunning => IsServiceRecentlyActive(ScraperService) && IsServiceRecentlyActive(VerifierService);

            private bool IsServiceRecentlyActive(ServiceStatus service)
            {
                // If the service is running, it's considered active
                if (service.IsActive)
                    return true;

                // If not running, check if last run was within the last 1 minute
                if (DateTime.TryParse(service.LastRunTime, out var lastRun))
                {
                    var diff = DateTime.UtcNow - lastRun.ToUniversalTime();
                    return diff.TotalMinutes >= 0 && diff.TotalMinutes <= 1;
                }
                return false;
            }
        }
    }
}
