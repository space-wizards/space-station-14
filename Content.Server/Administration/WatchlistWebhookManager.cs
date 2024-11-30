using Content.Server.Administration.Notes;
using Content.Server.Database;
using Content.Server.Discord;
using Content.Shared.CCVar;
using Robust.Server;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using System.Linq;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.Administration;

/// <summary>
///     This manager sends a Discord webhook notification whenever a player with an active
///     watchlist joins the server.
/// </summary>
public sealed class WatchlistWebhookManager : IPostInjectInit
{
    [Dependency] private readonly IAdminNotesManager _adminNotes = default!;
    [Dependency] private readonly IBaseServer _baseServer = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly DiscordWebhook _discord = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private ISawmill _sawmill = default!;

    private WebhookIdentifier? _webhookIdentifier;
    private List<WatchlistConnection> watchlistConnections = new();
    // true when a timer is running for the currently buffered connections, and another should not be started
    private bool buffering = false;

    void IPostInjectInit.PostInject()
    {
        _sawmill = Logger.GetSawmill("discord");

        var webhook = _cfg.GetCVar(CCVars.DiscordWatchlistConnectionWebhook);
        if (!string.IsNullOrWhiteSpace(webhook))
        {
            _discord.GetWebhook(webhook, data => _webhookIdentifier = data.ToIdentifier());
            _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
        }
    }

    private async void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus != SessionStatus.Connected)
            return;

        var watchlists = await _adminNotes.GetActiveWatchlists(e.Session.UserId);

        if (watchlists.Count == 0)
            return;

        watchlistConnections.Add(new WatchlistConnection(e.Session.Name, watchlists));

        var bufferTime = _cfg.GetCVar(CCVars.DiscordWatchlistConnectionBufferTime);

        if (bufferTime > 0f)
        {
            if (!buffering)
            {
                Timer.Spawn((int)(bufferTime * 1000f), OnBufferTimeElapsed);
                buffering = true;
            }
        }
        else
        {
            FlushConnections();
        }
    }

    private void OnBufferTimeElapsed()
    {
        buffering = false;
        FlushConnections();
    }

    private void FlushConnections()
    {
        SendDiscordMessage();

        // Clear the buffered list regardless of whether the message is sent successfully
        // This prevents infinitely buffering connections if we fail to send a message
        watchlistConnections.Clear();
    }

    private async void SendDiscordMessage()
    {
        try
        {
            if (_webhookIdentifier == null)
                return;

            var message = Loc.GetString("discord-watchlist-connection-header",
                    ("players", watchlistConnections.Count),
                    ("serverName", _baseServer.ServerName));

            foreach (var connection in watchlistConnections)
            {
                message += '\n';

                var watchlist = connection.Watchlists.First();
                var expiry = watchlist.ExpirationTime?.ToUnixTimeSeconds();

                if (expiry == null)
                {
                    if (connection.Watchlists.Count == 1)
                    {
                        message += Loc.GetString("discord-watchlist-connection-entry",
                            ("playerName", connection.PlayerName),
                            ("message", watchlist.Message));
                    }
                    else
                    {
                        message += Loc.GetString("discord-watchlist-connection-entry-more",
                            ("playerName", connection.PlayerName),
                            ("message", watchlist.Message),
                            ("otherWatchlists", connection.Watchlists.Count - 1));
                    }
                }
                else
                {
                    if (connection.Watchlists.Count == 1)
                    {
                        message += Loc.GetString("discord-watchlist-connection-entry-expires",
                            ("playerName", connection.PlayerName),
                            ("expiry", expiry),
                            ("message", watchlist.Message));
                    }
                    else
                    {
                        message += Loc.GetString("discord-watchlist-connection-entry-expires-more",
                            ("playerName", connection.PlayerName),
                            ("expiry", expiry),
                            ("message", watchlist.Message),
                            ("otherWatchlists", connection.Watchlists.Count - 1));
                    }
                }
            }

            var payload = new WebhookPayload { Content = message };

            await _discord.CreateMessage(_webhookIdentifier.Value, payload);
        }
        catch (Exception e)
        {
            _sawmill.Error($"Error while sending discord watchlist connection message:\n{e}");
        }
    }

    private sealed class WatchlistConnection
    {
        public string PlayerName;
        public List<AdminWatchlistRecord> Watchlists;

        public WatchlistConnection(string playerName, List<AdminWatchlistRecord> watchlists)
        {
            PlayerName = playerName;
            Watchlists = watchlists;
        }
    }
}
