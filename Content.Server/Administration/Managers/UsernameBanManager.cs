using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Chat.Managers;
using Content.Server.Database;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Network;

namespace Content.Server.Administration.Managers;

public interface IUsernameBanManager
{
    void Initialize();

    /// <summary>
    /// Checks if a username is banned and returns the ban message if it is.
    /// </summary>
    /// <param name="username">The username to check.</param>
    /// <param name="userId">The user ID of the player.</param>
    /// <param name="cancel">Cancellation token.</param>
    /// <returns>The ban message if the username is banned, null otherwise.</returns>
    Task<(bool IsBanned, string? BanMessage, bool AutoEscalate)> CheckUsernameAsync(
        string username,
        NetUserId userId,
        CancellationToken cancel = default);

    /// <summary>
    /// Refreshes the cached username bans from the database.
    /// </summary>
    Task RefreshCacheAsync();
}

public sealed class UsernameBanManager : IUsernameBanManager, IPostInjectInit
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IBanManager _banManager = default!;

    private ISawmill _sawmill = default!;

    private List<CachedRegexBan> _regexBans = [];
    private HashSet<string> _exactBans = [];
    private HashSet<string> _whitelisted = [];
    private readonly SemaphoreSlim _cacheLock = new(1, 1);

    public void Initialize()
    {
        // Initial cache load will happen on first check
    }

    public void PostInject()
    {
        _sawmill = _logManager.GetSawmill("admin.username_bans");
    }

    public async Task<(bool IsBanned, string? BanMessage, bool AutoEscalate)> CheckUsernameAsync(
        string username,
        NetUserId userId,
        CancellationToken cancel = default)
    {
        await EnsureCacheLoadedAsync(cancel);

        // Normalize username for comparison (case-insensitive)
        var normalizedUsername = username.ToLowerInvariant();

        // Check whitelist first
        if (_whitelisted.Contains(normalizedUsername))
        {
            _sawmill.Debug($"Username '{username}' is whitelisted");
            return (false, null, false);
        }

        // Check exact bans
        foreach (var exactBan in _exactBans)
        {
            if (normalizedUsername.Equals(exactBan, StringComparison.OrdinalIgnoreCase))
            {
                _sawmill.Info($"Username '{username}' ({userId}) hit exact ban: {exactBan}");
                var message = GetDefaultBanMessage();
                return (true, message, false);
            }
        }

        // Check regex bans
        foreach (var regexBan in _regexBans)
        {
            try
            {
                if (regexBan.Regex.IsMatch(username))
                {
                    _sawmill.Info($"Username '{username}' ({userId}) hit regex ban: {regexBan.Pattern} (AutoEscalate: {regexBan.AutoEscalate})");

                    var message = string.IsNullOrWhiteSpace(regexBan.CustomMessage)
                        ? GetDefaultBanMessage()
                        : regexBan.CustomMessage;

                    if (regexBan.AutoEscalate)
                    {
                        _sawmill.Warning($"Auto-escalating username ban for '{username}' ({userId}) due to regex pattern: {regexBan.Pattern}");
                        // Create a server ban asynchronously
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                var banInfo = new CreateServerBanInfo($"Auto-escalated from username ban: {regexBan.Pattern}");
                                banInfo.AddUser(userId, username);
                                // Make it permanent by not setting a duration
                                _banManager.CreateServerBan(banInfo);

                                _chat.SendAdminAlert(Loc.GetString("username-ban-auto-escalated",
                                    ("username", username),
                                    ("userId", userId),
                                    ("pattern", regexBan.Pattern)));
                            }
                            catch (Exception ex)
                            {
                                _sawmill.Error($"Failed to auto-escalate username ban: {ex}");
                            }
                        }, cancel);
                    }

                    return (true, message, regexBan.AutoEscalate);
                }
            }
            catch (RegexMatchTimeoutException ex)
            {
                _sawmill.Error($"Regex timeout while checking username '{username}' against pattern '{regexBan.Pattern}': {ex}");
                // Continue to next regex
            }
        }

        return (false, null, false);
    }

    public async Task RefreshCacheAsync()
    {
        await _cacheLock.WaitAsync();
        try
        {
            await RefreshCacheInternalAsync();
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    private async Task RefreshCacheInternalAsync()
    {
        _sawmill.Info("Refreshing username ban cache...");

        var regexBans = await _db.GetAllUsernameRegexBansAsync();
        var exactBans = await _db.GetAllUsernameExactBansAsync();
        var whitelists = await _db.GetAllUsernameWhitelistsAsync();

        var newRegexBans = new List<CachedRegexBan>();
        foreach (var ban in regexBans)
        {
            try
            {
                // Compile regex with timeout to prevent ReDoS attacks
                var regex = new Regex(
                    ban.Pattern,
                    RegexOptions.IgnoreCase | RegexOptions.Compiled,
                    TimeSpan.FromMilliseconds(100));

                newRegexBans.Add(new CachedRegexBan
                {
                    Pattern = ban.Pattern,
                    Regex = regex,
                    CustomMessage = ban.CustomMessage,
                    AutoEscalate = ban.AutoEscalate
                });
            }
            catch (Exception ex)
            {
                _sawmill.Error($"Failed to compile regex pattern '{ban.Pattern}': {ex}");
            }
        }

        _regexBans = newRegexBans;
        _exactBans = exactBans.Select(b => b.Username.ToLowerInvariant()).ToHashSet();
        _whitelisted = whitelists.Select(w => w.Username.ToLowerInvariant()).ToHashSet();

        _sawmill.Info($"Username ban cache refreshed: {_regexBans.Count} regex bans, {_exactBans.Count} exact bans, {_whitelisted.Count} whitelisted");
    }

    private bool _cacheLoaded = false;

    private async Task EnsureCacheLoadedAsync(CancellationToken cancel = default)
    {
        if (_cacheLoaded)
            return;

        await _cacheLock.WaitAsync(cancel);
        try
        {
            if (_cacheLoaded)
                return;

            await RefreshCacheInternalAsync();
            _cacheLoaded = true;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    private string GetDefaultBanMessage()
    {
        return Loc.GetString("username-ban-message");
    }

    private sealed class CachedRegexBan
    {
        public required string Pattern { get; init; }
        public required Regex Regex { get; init; }
        public string? CustomMessage { get; init; }
        public bool AutoEscalate { get; init; }
    }
}
