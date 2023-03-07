using Content.Server.Power.Components;
using Content.Server.Radio.Components;
using Content.Shared.Radio.Components;

namespace Content.Server.Radio.EntitySystems;

public sealed class TelecomSystem : EntitySystem
{
    public readonly HashSet<string> IgnoreChannels = new() { "CentCom", "Syndicate" };

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RadioReceiveAttemptEvent>(OnRadioReceiveAttempt);
    }

    private void OnRadioReceiveAttempt(ref RadioReceiveAttemptEvent args)
    {
        if (IgnoreChannels.Contains(args.Channel.ID))
            return;
        if (HasComp<RadioMicrophoneComponent>(args.RadioSource) && HasComp<RadioSpeakerComponent>(args.RadioReceiver))
            return;
        if (Transform(args.RadioSource).MapID != Transform(args.RadioReceiver).MapID)
        {
            args.Cancelled = true;
            return;
        }
        foreach (var (power, keys, _) in EntityQuery<ApcPowerReceiverComponent, EncryptionKeyHolderComponent, TelecomServerComponent>())
        {
            if (power.Powered && keys.Channels.Contains(args.Channel.ID))
                return;
        }
        args.Cancelled = true;
    }
}
