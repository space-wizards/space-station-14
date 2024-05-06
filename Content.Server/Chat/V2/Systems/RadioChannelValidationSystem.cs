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

        SubscribeLocalEvent<ChatValidationEvent<AttemptEquipmentRadioEvent>>((msg, args) => HandleRadioValidationEvent(msg, args));
        SubscribeLocalEvent<ChatValidationEvent<AttemptInternalRadioEvent>>((msg, args) => HandleRadioValidationEvent(msg, args));
    }

    private void HandleRadioValidationEvent(ChatValidationEvent<AttemptEquipmentRadioEvent> msg, EntitySessionEventArgs args)
    {
        if (!_proto.TryIndex<RadioChannelPrototype>(msg.Event.Channel, out _))
        {
            msg.Cancel(Loc.GetString(RadioChannelFailed, (RadioChannelKey, msg.Event.Channel)));

            return;
        }

        if (!TryComp<CanRadioUsingEquipmentComponent>(GetEntity(msg.Event.Sender), out var comp))
        {
            msg.Cancel(Loc.GetString(RadioChannelFailed, (RadioChannelKey, msg.Event.Channel)));

            return;
        }

        if (!comp.Channels.Contains(msg.Event.Channel))
        {
            msg.Cancel(Loc.GetString(RadioChannelFailed, (RadioChannelKey, msg.Event.Channel)));
        }
    }

    private void HandleRadioValidationEvent(ChatValidationEvent<AttemptInternalRadioEvent> msg, EntitySessionEventArgs args)
    {
        if (!_proto.TryIndex<RadioChannelPrototype>(msg.Event.Channel, out _))
        {
            msg.Cancel(Loc.GetString(RadioChannelFailed, (RadioChannelKey, msg.Event.Channel)));

            return;
        }

        if (!TryComp<CanRadioComponent>(GetEntity(msg.Event.Sender), out var comp))
        {
            msg.Cancel(Loc.GetString(RadioChannelFailed, (RadioChannelKey, msg.Event.Channel)));

            return;
        }

        if (!comp.SendChannels.Contains(msg.Event.Channel))
        {
            msg.Cancel(Loc.GetString(RadioChannelFailed, (RadioChannelKey, msg.Event.Channel)));
        }
    }
}
