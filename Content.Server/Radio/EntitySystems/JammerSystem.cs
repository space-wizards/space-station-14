using Content.Server.Radio.Components;

namespace Content.Server.Radio.EntitySystems;

public sealed class JammerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RadioReceiveAttemptEvent>(OnRadioSendAttempt);
    }

    private void OnRadioSendAttempt(ref RadioReceiveAttemptEvent args)
    {
        if (args.RadioSource == null)
            return;
        var source = Transform(args.RadioSource.Value).Coordinates;
        foreach (var jam in EntityQuery<RadioJammerComponent>())
        {
            if (jam.Enabled && source.InRange(EntityManager, Transform(jam.Owner).Coordinates, jam.Distance))
            {
                args.Cancelled = true;
                return;
            }
        }
    }
}
