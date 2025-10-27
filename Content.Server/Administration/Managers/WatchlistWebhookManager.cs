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
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private ISawmill _sawmill = default!;

    private string _webhookUrl = default!;
    private TimeSpan _bufferTime;

    private List<WatchlistConnection> watchlistConnections = new();
    private TimeSpan? _bufferStartTime;

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("discord");
        _cfg.OnValueChanged(CCVars.DiscordWatchlistConnectionBufferTime, SetBufferTime, true);
        _cfg.OnValueChanged(CCVars.DiscordWatchlistConnectionWebhook, SetWebhookUrl, true);
        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    private void SetBufferTime(float bufferTimeSeconds)
    {
        _bufferTime = TimeSpan.FromSeconds(bufferTimeSeconds);
    }

    private void SetWebhookUrl(string webhookUrl)
    {
        _webhookUrl = webhookUrl;
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
            if (_bufferStartTime == null)
                _bufferStartTime = _gameTiming.RealTime;
        }
        else
        {
            SendDiscordMessage();
        }
    }

    public void Update()
    {
        if (_bufferStartTime != null && _gameTiming.RealTime > (_bufferStartTime + _bufferTime))
        {
            SendDiscordMessage();
            _bufferStartTime = null;
        }
    }

    private async void SendDiscordMessage()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_webhookUrl))
                return;

            var webhookData = await _discord.GetWebhook(_webhookUrl);
            if (webhookData == null)
                return;

            var webhookIdentifier = webhookData.Value.ToIdentifier();

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

            await _discord.CreateMessage(webhookIdentifier, payload);
        }
        catch (Exception e)
        {
            _sawmill.Error($"Error while sending discord watchlist connection message:\n{e}");
        }

        // Clear the buffered list regardless of whether the message is sent successfully
        // This prevents infinitely buffering connections if we fail to send a message
        watchlistConnections.Clear();
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
