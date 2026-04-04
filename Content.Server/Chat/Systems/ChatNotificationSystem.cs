using Content.Server.Chat.Managers;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Chat.Systems;

/// <summary>
/// This system is used to notify specific players of the occurance of predefined events.
/// </summary>
public sealed partial class ChatNotificationSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IChatManager _chats = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private ISawmill _sawmill = default!;

    // The following data does not need to be saved

    // Local cache for rate limiting chat notifications by source
    // (Recipient, ChatNotification) -> Dictionary<Source, next allowed TOA>
    private readonly Dictionary<(EntityUid, ProtoId<ChatNotificationPrototype>), Dictionary<EntityUid, TimeSpan>> _chatNotificationsBySource = new();

    // Local cache for rate limiting chat notifications by type
    // (Recipient, ChatNotification) -> next allowed TOA
    private readonly Dictionary<(EntityUid, ProtoId<ChatNotificationPrototype>), TimeSpan> _chatNotificationsByType = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActorComponent, ChatNotificationEvent>(OnChatNotification);

        _sawmill = _logManager.GetSawmill("chatnotification");
    }

    /// <summary>
    /// Triggered when the specified player recieves a chat notification event.
    /// </summary>
    /// <param name="ent">The player receiving the chat notification.</param>
    /// <param name="args">The chat notification event</param>
    public void OnChatNotification(Entity<ActorComponent> ent, ref ChatNotificationEvent args)
    {
        if (!_proto.TryIndex(args.ChatNotification, out var chatNotification))
        {
            _sawmill.Warning("Attempted to index ChatNotificationPrototype " + args.ChatNotification + " but the prototype does not exist.");
            return;
        }

        var source = args.Source;
        var playerNotification = (ent, args.ChatNotification);

        // Exit without notifying the player if we received a notification before the appropriate time has elasped

        if (chatNotification.NotifyBySource)
        {
            if (!_chatNotificationsBySource.TryGetValue(playerNotification, out var trackedSources))
                trackedSources = new();

            trackedSources.TryGetValue(source, out var timeSpan);
            trackedSources[source] = _timing.CurTime + chatNotification.NextDelay;

            _chatNotificationsBySource[playerNotification] = trackedSources;

            if (_timing.CurTime < timeSpan)
                return;
        }
        else
        {
            _chatNotificationsByType.TryGetValue(playerNotification, out var timeSpan);
            _chatNotificationsByType[playerNotification] = _timing.CurTime + chatNotification.NextDelay;

            if (_timing.CurTime < timeSpan)
                return;
        }

        var sourceName = args.SourceNameOverride ?? Name(source);
        var userName = args.UserNameOverride ?? (args.User.HasValue ? Name(args.User.Value) : string.Empty);
        var targetName = Name(ent);

        var message = Loc.GetString(chatNotification.Message, ("source", sourceName), ("user", userName), ("target", targetName));
        var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));

        _chats.ChatMessageToOne(
            ChatChannel.Notifications,
            message,
            wrappedMessage,
            default,
            false,
            ent.Comp.PlayerSession.Channel,
            colorOverride: chatNotification.Color
        );

        if (chatNotification.Sound != null && _mind.TryGetMind(ent, out var mindId, out _))
            _roles.MindPlaySound(mindId, chatNotification.Sound);
    }
}
