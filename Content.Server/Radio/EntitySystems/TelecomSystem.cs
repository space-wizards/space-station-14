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
        if (args.Channel.longRange)
            return;
        if (HasComp<RadioMicrophoneComponent>(args.RadioSource) && HasComp<RadioSpeakerComponent>(args.RadioReceiver))
            return;
        var mapSource = Transform(args.RadioSource).MapID;
        var mapReceiver = Transform(args.RadioReceiver).MapID;
        if (mapSource == mapReceiver)
        {
            var map = mapSource;
            var servers = EntityQuery<ApcPowerReceiverComponent, EncryptionKeyHolderComponent, TransformComponent, TelecomServerComponent>();
            foreach (var (power, keys, transform, _) in servers)
            {
                if (transform.MapID == map && power.Powered && keys.Channels.Contains(args.Channel.ID))
                    return;
            }
        }
        args.Cancelled = true;
    }
}
