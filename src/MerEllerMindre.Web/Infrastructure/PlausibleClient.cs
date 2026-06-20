using System.Text;
using System.Text.Json;

namespace MerEllerMindre.Web.Infrastructure;

/// <summary>
/// Fires gameplay goals to Plausible's keyless Events API, forwarding the visitor's UA + IP so
/// the event lands on the right visit. No-op outside Production so local `dotnet run` doesn't
/// pollute stats. Fire-and-forget: a dropped event is just a missing count.
/// ponytail: no retry/queue. Add resilience only if events measurably drop.
/// </summary>
public sealed class PlausibleClient
{
    private readonly HttpClient _http;
    private readonly string _domain;
    private readonly bool _enabled;

    public PlausibleClient(HttpClient http, IConfiguration config, IWebHostEnvironment env)
    {
        _http = http;
        _domain = config["Plausible:Domain"] ?? "kvissig.se";
        _enabled = env.IsProduction();
    }

    public void Track(string eventName, HttpContext ctx)
    {
        if (!_enabled)
            return;

        var url = $"{ctx.Request.Scheme}://{ctx.Request.Host}{ctx.Request.Path}";
        var body = JsonSerializer.Serialize(new { name = eventName, url, domain = _domain });
        var req = new HttpRequestMessage(HttpMethod.Post, "https://plausible.io/api/event")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        req.Headers.TryAddWithoutValidation("User-Agent", ctx.Request.Headers.UserAgent.ToString());
        if (ctx.Connection.RemoteIpAddress is { } ip)
            req.Headers.TryAddWithoutValidation("X-Forwarded-For", ip.ToString());

        _ = _http.SendAsync(req);
    }
}
