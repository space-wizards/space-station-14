using Content.Shared.Chat.V2;
using Content.Shared.Chat.V2.Components;
using Content.Shared.Chat.V2.Systems;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Server.Chat.V2.Systems;

public sealed class RadioChannelValidationSystem : EntitySystem
{
    private const string RadioChannelFailed = "chat-system-radio-channel-failed";
    private const string RadioChannelKey = "channel";

    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChatSentEvent<VerbalChatSentEvent>>(OnValidateAttemptVerbalChatEvent);
    }

    private void OnValidateAttemptVerbalChatEvent(ChatSentEvent<VerbalChatSentEvent> msg, EntitySessionEventArgs args)
    {
        if (msg.Event.RadioChannel == null)
            return;

        if (!_proto.TryIndex(msg.Event.RadioChannel, out _) ||
            !TryComp<CanRadioUsingEquipmentComponent>(GetEntity(msg.Event.Sender), out var comp) ||
            !comp.Channels.Contains(msg.Event.RadioChannel)
        )
            msg.Cancel(Loc.GetString(RadioChannelFailed, (RadioChannelKey, msg.Event.RadioChannel)));
    }
}
