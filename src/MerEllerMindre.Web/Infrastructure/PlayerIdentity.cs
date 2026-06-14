using Microsoft.AspNetCore.DataProtection;

namespace MerEllerMindre.Web.Infrastructure;

/// <summary>
/// Per-game player identity carried in an encrypted cookie (ASP.NET Data Protection).
/// On OpenLobby/JoinGame the shell mints a playerId and stores it here; the lobby and the
/// 2 s poll read it back to tag "you" (and the host as "värd · du"). One cookie per game so
/// a device can host one game and join another without clobbering identities.
/// </summary>
public sealed class PlayerIdentity
{
    private const string Purpose = "MerEllerMindre.PlayerIdentity.v1";
    private readonly IDataProtector _protector;

    public PlayerIdentity(IDataProtectionProvider provider) =>
        _protector = provider.CreateProtector(Purpose);

    /// <summary>Store the current player's id for a game in an encrypted cookie.</summary>
    public void SetPlayer(HttpContext http, Guid gameId, Guid playerId)
    {
        var token = _protector.Protect(playerId.ToString("N"));
        http.Response.Cookies.Append(CookieName(gameId), token, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            Path = "/"
        });
    }

    /// <summary>Read back the player's id for a game, if a valid cookie is present.</summary>
    public Guid? GetPlayer(HttpContext http, Guid gameId)
    {
        if (!http.Request.Cookies.TryGetValue(CookieName(gameId), out var token) || string.IsNullOrEmpty(token))
            return null;

        try
        {
            return Guid.ParseExact(_protector.Unprotect(token), "N");
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            return null; // tampered or stale key — treat as no identity
        }
    }

    private static string CookieName(Guid gameId) => $"mem_player_{gameId:N}";
}
