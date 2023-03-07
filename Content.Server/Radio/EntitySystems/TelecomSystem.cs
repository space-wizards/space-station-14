using Content.Shared.Radio.Components;

namespace Content.Server.Radio.EntitySystems;

public sealed class TelecomSystem : EntitySystem
{
    public readonly HashSet<string> IgnoreChannelIds = new() { "CentCom", "Syndicate" };

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RadioReceiveAttemptEvent>(OnRadioReceiveAttempt);
    }

    private void OnRadioReceiveAttempt(RadioReceiveAttemptEvent args)
    {
        if (IgnoreChannelIds.Contains(args.Channel.ID))
            return;
        foreach (var (_, keys) in EntityQuery<TelecomServerComponent, EncryptionKeyHolderComponent>())
        {
            if (keys.Channels.Contains(args.Channel.ID))
                return;
        }
        args.Cancel();
    }
}
