using Content.Server.Power.Components;
using Content.Server.Radio.Components;
using Content.Shared.Radio.Components;

namespace Content.Server.Radio.EntitySystems;

public sealed class TelecomSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RadioReceiveAttemptEvent>(OnRadioReceiveAttempt);
    }

    private void OnRadioReceiveAttempt(ref RadioReceiveAttemptEvent args)
    {
        if (args.Channel.LongRange)
            return;
        // if both source and receiver (for example) are handheld radios (but not headsets) then you don't server 
        if (HasComp<RadioMicrophoneComponent>(args.RadioSource) && HasComp<RadioSpeakerComponent>(args.RadioReceiver))
            return;
        var servers = EntityQuery<TelecomServerComponent, EncryptionKeyHolderComponent, ApcPowerReceiverComponent, TransformComponent>();
        foreach (var (_, keys, power, transform) in servers)
        {
            if (transform.MapID == args.MapId && power.Powered && keys.Channels.Contains(args.Channel.ID))
                return;
        }
    }
}
