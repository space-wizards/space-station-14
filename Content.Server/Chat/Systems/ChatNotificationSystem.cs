using Content.Server.Chat.Managers;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Chat.Systems;

public sealed partial class ChatNotificationSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IChatManager _chats = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    // The following data does not need to be saved

    // For storing chat notifications by source
    // (Recipient, ChatNotification) -> Dictionary<Source, last TOA>
    Dictionary<(EntityUid, ProtoId<ChatNotificationPrototype>), Dictionary<EntityUid, TimeSpan>> _chatNotificationsBySource = new();

    // For storing chat notifications by type
    // (Recipient, ChatNotification) -> last TOA
    Dictionary<(EntityUid, ProtoId<ChatNotificationPrototype>), TimeSpan> _chatNotificationsByType = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActorComponent, ChatNotificationEvent>(OnChatNotification);
    }

    public void OnChatNotification(Entity<ActorComponent> ent, ref ChatNotificationEvent args)
    {
        if (!_proto.TryIndex(args.ChatNotification, out var chatNotification))
            return;

        var source = args.Source;

        // If we receive a notification before the delay time has elasped, update the timer and exit

        if (chatNotification.NotifyBySource)
        {
            if (!_chatNotificationsBySource.TryGetValue((ent, chatNotification), out var trackedSources))
                trackedSources = new();

            trackedSources.TryGetValue(source, out var timeSpan);
            trackedSources[source] = _timing.CurTime;

            _chatNotificationsBySource[(ent, chatNotification)] = trackedSources;

            if (_timing.CurTime < (timeSpan + TimeSpan.FromSeconds(chatNotification.NextDelay)))
                return;
        }

        else
        {
            _chatNotificationsByType.TryGetValue((ent, chatNotification), out var timeSpan);
            _chatNotificationsByType[(ent, chatNotification)] = _timing.CurTime;

            if (_timing.CurTime < (timeSpan + TimeSpan.FromSeconds(chatNotification.NextDelay)))
                return;
        }

        var sourceName = args.SourceNameOverride ?? Name(source);
        var userName = args.UserNameOverride ?? (args.User.HasValue ? Name(args.User.Value) : string.Empty);
        var targetName = Name(ent);

        var message = Loc.GetString(chatNotification.Message, ("source", sourceName), ("user", userName), ("target", targetName));
        var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));

        _chats.ChatMessageToOne
            (ChatChannel.Notifications, message, wrappedMessage, default, false, ent.Comp.PlayerSession.Channel, colorOverride: chatNotification.Color);

        if (chatNotification.Sound != null && _mind.TryGetMind(ent, out var mindId, out _))
            _roles.MindPlaySound(mindId, chatNotification.Sound);
    }
}
