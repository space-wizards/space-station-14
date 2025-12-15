using System.Linq;
using Content.Server.Database;
using Content.Shared.Administration.Managers.Bwoink;
using Content.Shared.Administration.Managers.Bwoink.Features;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Player;

namespace Content.Server.Administration.Managers.Bwoink;

/// <summary>
/// This manager manages the <see cref="CCVars.AdminAhelpOverrideClientName"/> along with the banned / disconnected / re-connected message thingies.
/// </summary>
/// <remarks>
/// Currently the AdminAhelpOverrideClientName is global, and will override the name in all channels. The reason is that I cannot be arsed to actually fix it right nyow (out of scope)
/// </remarks>
public sealed class MessageBwoinkManager
{
    [Dependency] private readonly ServerBwoinkManager _bwoinkManager = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IServerDbManager _dbManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly ILocalizationManager _localizationManager = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

    // ReSharper disable once InconsistentNaming - All I hear is "mrrp meow :3 :3 :3"
    private ISawmill Log = null!;
    /// <summary>
    /// Time that determines how old the last message must be for sending a status message.
    /// </summary>
    private static TimeSpan MessageTimeout = TimeSpan.FromMinutes(5);

    private string _overrideName = string.Empty;

    public void Initialize()
    {
        Log = _logManager.GetSawmill("bwoink.messages");

        _bwoinkManager.MessageBeingSent += MessageBeingSent;
        _configurationManager.OnValueChanged(CCVars.AdminAhelpOverrideClientName, s => _overrideName = s);
        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    private async void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        try
        {
            var channels = _bwoinkManager.GetBwoinkChannelsWithFeature<StatusMessages>().ToList();
            if (channels.Count == 0)
                return;

            // have we had any messages before that?
            var hadMessage = false;
            foreach (var (id, _) in channels)
            {
                if (!_bwoinkManager.Conversations[id].TryGetValue(e.Session.UserId, out var conversation))
                    continue;

                if (DateTime.UtcNow - conversation.Messages.Last().SentAt > MessageTimeout)
                    continue;

                hadMessage = true;
            }

            if (!hadMessage)
                return;

            ServerBanDef? banInfo = null;
            if (e.NewStatus == SessionStatus.Disconnected)
            {
                banInfo = await _dbManager.GetServerBanAsync(null, e.Session.UserId, null, null);
            }

            string? message = null;
            var type = BwoinkStatusTypes.Reconnect;
            Color? color = null;
            switch (e.NewStatus)
            {
                case SessionStatus.Connected:
                    message = _localizationManager.GetString("bwoink-system-player-reconnecting", ("name", e.Session.Name));
                    type = BwoinkStatusTypes.Reconnect;
                    color = Color.Green;
                    break;
                case SessionStatus.Disconnected when banInfo != null:
                    message = _localizationManager.GetString("bwoink-system-player-banned", ("name", e.Session.Name), ("banReason", banInfo.Reason));
                    type = BwoinkStatusTypes.Banned;
                    color = Color.Orange;
                    break;
                case SessionStatus.Disconnected:
                    message = _localizationManager.GetString("bwoink-system-player-disconnecting", ("name", e.Session.Name));
                    type = BwoinkStatusTypes.Disconnect;
                    color = Color.Yellow;
                    break;
            }

            if (message == null)
                return;

            var roundTime = TimeSpan.MinValue;
            var roundId = -1;

            if (_entitySystemManager.TryGetEntitySystem<SharedGameTicker>(out var gameTicker))
            {
                roundTime = gameTicker.RoundDuration();
                roundId = gameTicker.RoundId;
            }

            // Since I want to avoid adding a morbillion fields to the message object, we cannot just set an "icon" property.
            // The solution?
            // Hijack the sender to indicate why this message was sent so the BwoinkDiscordRelayManager can check it and set the relevant emoji.
            // Please do not murder me for this.
            var bwoinkMessage = new BwoinkMessage(((byte)type).ToString(),
                null,
                DateTime.UtcNow,
                message,
                MessageFlags.GenericSystem | MessageFlags.ManagerOnly,
                roundTime,
                roundId,
                color);

            foreach (var (id, _) in channels)
            {
                _bwoinkManager.SendMessageInChannel(id, e.Session.UserId, bwoinkMessage);
            }
        }
        catch (Exception exception)
        {
            Log.Error(exception.ToString());
        }
    }

    private void MessageBeingSent(BwoinkMessageClientSentEventArgs mrrpMeow)
    {
        if (string.IsNullOrWhiteSpace(_overrideName))
            return;

        if (!mrrpMeow.Message.Flags.HasFlag(MessageFlags.Manager))
            return;

        mrrpMeow.Message = mrrpMeow.Message with { Sender = _overrideName };
    }

    /// <summary>
    /// Holds the types used as the sender for messages made by this manager. This is to allow interop with the <see cref="BwoinkDiscordRelayManager"/>
    /// </summary>
    public enum BwoinkStatusTypes : byte
    {
        Banned = 0,
        Disconnect = 1,
        Reconnect = 2,
    }
}
