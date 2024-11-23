using Content.Server.Administration.Notes;
using Content.Server.Database;
using Content.Server.Discord;
using Content.Shared.CCVar;
using Robust.Server;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using System.Linq;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.Administration;

/// <summary>
///     This system sends a Discord webhook notification whenever a player with an active
///     watchlist joins the server.
/// </summary>
public sealed class WatchlistWebhookSystem : EntitySystem
{
    [Dependency] private readonly IAdminNotesManager _adminNotes = default!;
    [Dependency] private readonly IBaseServer _baseServer = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly DiscordWebhook _discord = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private WebhookIdentifier? _webhookIdentifier;
    private List<WatchlistConnection> watchlistConnections = new();
    // true when a timer is running for the currently buffered connections, and another should not be started
    private bool buffering = false;

    public override void Initialize()
    {
        Subs.CVar(_cfg, CCVars.DiscordWatchlistConnectionWebhook, SetWebhookIdentifier, true);
        _netManager.Connected += OnConnected;
        base.Initialize();
    }

    private void SetWebhookIdentifier(string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            _discord.GetWebhook(value, data => _webhookIdentifier = data.ToIdentifier());
    }

    private async void OnConnected(object? sender, NetChannelArgs e)
    {
        var watchlists = await _adminNotes.GetActiveWatchlists(e.Channel.UserId);

        if (watchlists.Count == 0)
            return;

        var session = _playerManager.GetSessionByChannel(e.Channel);
        watchlistConnections.Add(new WatchlistConnection(session.Name, watchlists));

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
                message += Loc.GetString("discord-watchlist-connection-entry", ("playerName", connection.PlayerName));
                message += ' ';

                var watchlist = connection.Watchlists.First();
                var expirationTime = watchlist.ExpirationTime;
                if (expirationTime != null)
                    message += Loc.GetString("discord-watchlist-connection-expiry",
                            ("expiry", ((DateTimeOffset)expirationTime).ToUnixTimeSeconds()));
                else
                    message += Loc.GetString("discord-watchlist-connection-noexpiry");

                message += ' ';
                message += Loc.GetString("discord-watchlist-connection-message", ("message", watchlist.Message));

                if (connection.Watchlists.Count > 1)
                {
                    message += ' ';
                    message += Loc.GetString("discord-watchlist-connection-more", ("watchlists", connection.Watchlists.Count - 1));
                }
            }

            var payload = new WebhookPayload { Content = message };

            await _discord.CreateMessage(_webhookIdentifier.Value, payload);
        }
        catch (Exception e)
        {
            Log.Error($"Error while sending discord watchlist connection message:\n{e}");
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
