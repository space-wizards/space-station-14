using Content.Server.Administration.Managers;
using Content.Shared.Chat.V2.Components;
using Content.Shared.Chat.V2.Systems;
using Content.Shared.Ghost;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.Chat.V2.Systems;

public sealed class DeadChatValidationSystem : EntitySystem
{
    private const string DeadChatFailed = "chat-system-dead-chat-failed";
    private string _deadChatFailed = "";

    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        _deadChatFailed = Loc.GetString(DeadChatFailed);

        SubscribeLocalEvent<ChatSentEvent<OutOfCharacterChatSentEvent>>(OnValidateAttemptOutOfCharacterChatEvent);
    }

    private void OnValidateAttemptOutOfCharacterChatEvent(ChatSentEvent<OutOfCharacterChatSentEvent> msg, EntitySessionEventArgs args)
    {
        if (!_proto.TryIndex(msg.Event.Channel, out var proto))
            msg.Cancel(_deadChatFailed);

        if (!proto!.ObserversOnly)
            return;

        var entityUid = GetEntity(msg.Event.Sender);
        if (_admin.IsAdmin(entityUid))
            return;

        if (!HasComp<GhostComponent>(entityUid) && !_mobState.IsDead(entityUid))
            msg.Cancel(_deadChatFailed);
    }
}
