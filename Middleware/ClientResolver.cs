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
        _allowed = new HashSet<string>(list, StringComparer.OrdinalIgnoreCase);

        _defaultSubdomain = config["Clients:DefaultSubdomain"];
    }

    /// <summary>
    /// Returns the subdomain for the current request, validated against the allowlist.
    /// Returns null when no candidate resolves.
    /// </summary>
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

    /// <summary>
    /// Resolves the client's Guid from the subdomain (cached for 1 request + static cache).
    /// </summary>
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
}