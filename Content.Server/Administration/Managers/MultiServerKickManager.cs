using System.Text.Json;
using System.Text.Json.Serialization;
using Content.Server.Database;
using Content.Shared.CCVar;
using Robust.Server.Player;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.Administration.Managers;

/// <summary>
/// Handles kicking people that connect to multiple servers on the same DB at once.
/// </summary>
/// <seealso cref="CCVars.AdminAllowMultiServerPlay"/>
public sealed class MultiServerKickManager
{
    public const string NotificationChannel = "multi_server_kick";

    [Dependency] private readonly IPlayerManager _playerManager = null!;
    [Dependency] private readonly IServerDbManager _dbManager = null!;
    [Dependency] private readonly ILogManager _logManager = null!;
    [Dependency] private readonly IConfigurationManager _cfg = null!;
    [Dependency] private readonly IAdminManager _adminManager = null!;
    [Dependency] private readonly ITaskManager _taskManager = null!;
    [Dependency] private readonly IServerNetManager _netManager = null!;
    [Dependency] private readonly ILocalizationManager _loc = null!;
    [Dependency] private readonly ServerDbEntryManager _serverDbEntry = null!;

    private ISawmill _sawmill = null!;
    private bool _allowed;

    public void Initialize()
    {
        _sawmill = _logManager.GetSawmill("multi_server_kick");

        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
        _cfg.OnValueChanged(CCVars.AdminAllowMultiServerPlay, b => _allowed = b, true);

        _dbManager.SubscribeToJsonNotification<NotificationData>(
            _taskManager,
            _sawmill,
            NotificationChannel,
            OnNotification,
            OnNotificationEarlyFilter
        );
    }

    // ReSharper disable once AsyncVoidMethod
    private async void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (_allowed)
            return;

        if (e.NewStatus != SessionStatus.InGame)
            return;

        // Send notification to other servers so they can kick this player that just connected.
        try
        {
            await _dbManager.SendNotification(new DatabaseNotification
            {
                Channel = NotificationChannel,
                Payload = JsonSerializer.Serialize(new NotificationData
                {
                    PlayerId = e.Session.UserId,
                    ServerId = (await _serverDbEntry.ServerEntity).Id,
                }),
            });
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Failed to send notification for multi server kick: {ex}");
        }
    }

    private bool OnNotificationEarlyFilter()
    {
        if (_allowed)
        {
            _sawmill.Verbose("Received notification for player join, but multi server play is allowed on this server. Ignoring");
            return false;
        }

        return true;
    }

    // ReSharper disable once AsyncVoidMethod
    private async void OnNotification(NotificationData notification)
    {
        if (!_playerManager.TryGetSessionById(new NetUserId(notification.PlayerId), out var player))
            return;

        if (notification.ServerId == (await _serverDbEntry.ServerEntity).Id)
            return;

        if (_adminManager.IsAdmin(player, includeDeAdmin: true))
            return;

        _sawmill.Info($"Kicking {player} for connecting to another server. Multi-server play is not allowed.");
        _netManager.DisconnectChannel(player.Channel, _loc.GetString("multi-server-kick-reason"));
    }

    private sealed class NotificationData
    {
        [JsonPropertyName("player_id")]
        public Guid PlayerId { get; set; }

        [JsonPropertyName("server_id")]
        public int ServerId { get; set; }
    }
}
