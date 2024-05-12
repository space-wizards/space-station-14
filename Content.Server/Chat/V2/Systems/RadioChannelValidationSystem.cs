using Content.Shared.Chat.V2;
using Content.Shared.Chat.V2.Components;
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

        SubscribeLocalEvent<ChatValidationEvent<AttemptEquipmentRadioEvent>>(OnValidateAttemptEquipmentRadioEvent);
        SubscribeLocalEvent<ChatValidationEvent<AttemptInternalRadioEvent>>(OnValidateAttemptInternalRadioEvent);
    }

    private void OnValidateAttemptEquipmentRadioEvent(ChatValidationEvent<AttemptEquipmentRadioEvent> msg, EntitySessionEventArgs args)
    {
        if (!_proto.TryIndex(msg.Event.Channel, out _) ||
            !TryComp<CanRadioUsingEquipmentComponent>(GetEntity(msg.Event.Sender), out var comp) ||
            !comp.Channels.Contains(msg.Event.Channel))
            msg.Cancel(Loc.GetString(RadioChannelFailed, (RadioChannelKey, msg.Event.Channel)));
    }

    private void OnValidateAttemptInternalRadioEvent(ChatValidationEvent<AttemptInternalRadioEvent> msg, EntitySessionEventArgs args)
    {
        if (!_proto.TryIndex(msg.Event.Channel, out _) ||
            !TryComp<CanRadioComponent>(GetEntity(msg.Event.Sender), out var comp) ||
            !comp.SendChannels.Contains(msg.Event.Channel)
        )
            msg.Cancel(Loc.GetString(RadioChannelFailed, (RadioChannelKey, msg.Event.Channel)));
    }
}
