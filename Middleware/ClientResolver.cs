using Microsoft.Extensions.Configuration;
using SmashPointBookingAPI.Interfaces;

namespace SmashPointBookingAPI.Middleware;

public class ClientResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _config;
    private readonly HashSet<string> _allowed;
    private readonly string? _defaultSubdomain;

    private static readonly Dictionary<string, Guid> _cache = new(StringComparer.OrdinalIgnoreCase);

    public ClientResolver(IHttpContextAccessor httpContextAccessor, IConfiguration config)
    {
        _httpContextAccessor = httpContextAccessor;
        _config = config;

        var list = config.GetSection("Clients:AllowedSubdomains").Get<string[]>()
                   ?? Array.Empty<string>();

        // If no allowlist is configured, fall back to the default subdomain so that
        // a missing or misconfigured appsettings.json does not brick the whole app.
        var allowedList = list.Length > 0
            ? list
            : new[] { config["Clients:DefaultSubdomain"] ?? "smashpoint" };

        _allowed = new HashSet<string>(allowedList, StringComparer.OrdinalIgnoreCase);
        _defaultSubdomain = config["Clients:DefaultSubdomain"];
    }

    public string? GetSubdomain()
    {
        var ctx = _httpContextAccessor.HttpContext;
        if (ctx is null) return null;

        if (ctx.Items["Subdomain"] is string cached && !string.IsNullOrEmpty(cached))
            return cached;

        var candidate = ctx.Request.Headers["X-Client-Subdomain"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(candidate))
        {
            var host = ctx.Request.Host.Host;
            if (!string.IsNullOrEmpty(host))
            {
                var parts = host.Split('.');
                if (parts.Length >= 2 && parts[0] != "www" && parts[0] != "localhost")
                    candidate = parts[0];
            }
        }

        if (string.IsNullOrWhiteSpace(candidate))
            candidate = _defaultSubdomain;

        if (string.IsNullOrWhiteSpace(candidate))
            return null;

        candidate = candidate.Trim().ToLowerInvariant();

        if (!_allowed.Contains(candidate))
            return null;

        ctx.Items["Subdomain"] = candidate;
        return candidate;
    }

    public async Task<Guid?> ResolveClientIdAsync()
    {
        var ctx = _httpContextAccessor.HttpContext;
        if (ctx is null) return null;

        if (ctx.Items["ClientId"] is Guid cached) return cached;

        var subdomain = GetSubdomain();
        if (string.IsNullOrEmpty(subdomain)) return null;

        if (_cache.TryGetValue(subdomain, out var cachedClientId))
        {
            ctx.Items["ClientId"] = cachedClientId;
            return cachedClientId;
        }

        var clientService = ctx.RequestServices.GetService<IClientService>();
        if (clientService is null) return null;

        try
        {
            var clientId = await clientService.GetClientIdBySubdomainAsync(subdomain);
            _cache[subdomain] = clientId;
            ctx.Items["ClientId"] = clientId;
            return clientId;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Static helper — reads allowlist from configuration and resolves subdomain
    /// without needing DI. Same fallback behavior as the instance method.
    /// </summary>
    public static string? ResolveSubdomain(HttpContext ctx, IConfiguration config)
    {
        var list = config.GetSection("Clients:AllowedSubdomains").Get<string[]>()
                   ?? Array.Empty<string>();
        var defaultSub = config["Clients:DefaultSubdomain"];

        var allowedList = list.Length > 0
            ? list
            : new[] { defaultSub ?? "smashpoint" };
        var allowed = new HashSet<string>(allowedList, StringComparer.OrdinalIgnoreCase);

        var fromHeader = ctx.Request.Headers["X-Client-Subdomain"].FirstOrDefault();
        var candidate = fromHeader;

        if (string.IsNullOrWhiteSpace(candidate))
        {
            var host = ctx.Request.Host.Host;
            if (!string.IsNullOrEmpty(host))
            {
                var parts = host.Split('.');
                if (parts.Length >= 2 && parts[0] != "www" && parts[0] != "localhost")
                    candidate = parts[0];
            }
        }

        if (string.IsNullOrWhiteSpace(candidate))
            candidate = defaultSub;

        if (string.IsNullOrWhiteSpace(candidate))
            return null;

        candidate = candidate.Trim().ToLowerInvariant();
        return allowed.Contains(candidate) ? candidate : null;
    }
}