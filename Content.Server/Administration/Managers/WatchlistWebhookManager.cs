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
using Robust.Shared.Timing;
using System.Linq;
using System.Text;

namespace Content.Server.Administration.Managers;

/// <summary>
///     This manager sends a Discord webhook notification whenever a player with an active
///     watchlist joins the server.
/// </summary>
public sealed class WatchlistWebhookManager : IWatchlistWebhookManager
{
    [Dependency] private readonly IAdminNotesManager _adminNotes = default!;
    [Dependency] private readonly IBaseServer _baseServer = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly DiscordWebhook _discord = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private ISawmill _sawmill = default!;

    private WebhookIdentifier? _webhookIdentifier;
    private List<WatchlistConnection> watchlistConnections = new();
    private TimeSpan _bufferTime;
    private RStopwatch? _bufferingStopwatch;

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("discord");

        var bufferTimeSeconds = _cfg.GetCVar(CCVars.DiscordWatchlistConnectionBufferTime);
        _bufferTime = TimeSpan.FromSeconds(bufferTimeSeconds);

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

        if (_bufferTime > TimeSpan.Zero)
        {
            if (_bufferingStopwatch == null)
                _bufferingStopwatch = RStopwatch.StartNew();
        }
        else
        {
            FlushConnections();
        }
    }

    public void Update()
    {
        if (_bufferingStopwatch != null && ((RStopwatch)_bufferingStopwatch).Elapsed > _bufferTime)
        {
            FlushConnections();
            _bufferingStopwatch = null;
        }
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

            var messageBuilder = new StringBuilder(Loc.GetString("discord-watchlist-connection-header",
                    ("players", watchlistConnections.Count),
                    ("serverName", _baseServer.ServerName)));

            foreach (var connection in watchlistConnections)
            {
                messageBuilder.Append('\n');

                var watchlist = connection.Watchlists.First();
                var expiry = watchlist.ExpirationTime?.ToUnixTimeSeconds();
                messageBuilder.Append(Loc.GetString("discord-watchlist-connection-entry",
                    ("playerName", connection.PlayerName),
                    ("message", watchlist.Message),
                    ("expiry", expiry ?? 0),
                    ("otherWatchlists", connection.Watchlists.Count - 1)));
            }

            var payload = new WebhookPayload { Content = messageBuilder.ToString() };

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
