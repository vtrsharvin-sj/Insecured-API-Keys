namespace UnsecuredAPIKeys.Providers;

/// <summary>
/// Thrown by GitHubSearchProvider when the token hits GitHub's search rate limit
/// and the reset time is more than 1 minute away. The scraper catches this,
/// records the reset time on the token so it gets skipped, then picks another token.
/// </summary>
public class GitHubRateLimitException(DateTime resetAtUTC) : Exception($"GitHub rate limit exceeded. Resets at {resetAtUTC:O}")
{
    public DateTime ResetAtUTC { get; } = resetAtUTC;
}
