using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace UnsecuredAPIKeys.WebAPI.Commons;

public class AllowedReferrersAttribute : TypeFilterAttribute
{
    private static readonly List<string> _allowedReferrers = BuildAllowedReferrers();

    // Defaults plus any ALLOWED_REFERRERS env list (comma separated, e.g. "myapp.example.com,myapp2.example.com").
    // Requests whose Referer points back at the host serving the request are always allowed
    // (same-origin = UI proxied through the same domain as the API).
    private static List<string> BuildAllowedReferrers()
    {
        var allowed = new List<string>
        {
            "localhost:5173",
            "localhost:7227",
            "localhost:3000",
            "localhost:3001",
            "localhost:8080",
            "localhost:8081"
        };

        var envReferrers = Environment.GetEnvironmentVariable("ALLOWED_REFERRERS");
        if (!string.IsNullOrEmpty(envReferrers))
        {
            allowed.AddRange(envReferrers
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        return allowed;
    }

    public AllowedReferrersAttribute(params string[] additionalReferrers)
        : base(typeof(AllowedReferrersFilter))
    {
        Arguments = [_allowedReferrers.Concat(additionalReferrers).ToArray()];
    }

    private class AllowedReferrersFilter(string[] allowedReferrers) : IActionFilter
    {
        private readonly string[] _filterAllowedReferrers = allowedReferrers;

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var referrer = context.HttpContext.Request.Headers["Referer"];
            if (!string.IsNullOrEmpty(referrer))
            {
                var referrerUri = new Uri(referrer!);
                var referrerDomain = referrerUri.Authority.ToLower();

                // Same-origin requests (UI proxied on the same host as the API) are always allowed.
                var requestHost = context.HttpContext.Request.Host.Value?.ToLower();
                if (referrerDomain == requestHost)
                {
                    return;
                }

                if (!_filterAllowedReferrers.Any(allowedReferrer => referrerDomain.EndsWith(allowedReferrer, StringComparison.OrdinalIgnoreCase)))
                {
                    context.Result = new StatusCodeResult(403); // Forbidden
                }
            }
            else
            {
                if (!(context.HttpContext.Request.Host.Value?.Contains("localhost") ?? false)) context.Result = new StatusCodeResult(403); // Forbidden
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // Not needed in this case
        }
    }
}
