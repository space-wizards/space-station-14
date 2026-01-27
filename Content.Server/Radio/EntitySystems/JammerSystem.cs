using Content.Shared.Radio.EntitySystems;
using Content.Shared.Radio.Components;

namespace Content.Server.Radio.EntitySystems;

public sealed class JammerSystem : SharedJammerSystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RadioSendAttemptEvent>(OnRadioSendAttempt);
        SubscribeLocalEvent<RadioReceiveAttemptEvent>(OnRadioReceiveAttempt);
    }

    private void OnRadioSendAttempt(ref RadioSendAttemptEvent args)
    {
        if (ShouldCancel(args.RadioSource, args.Channel.Frequency))
            args.Cancelled = true;
    }

    private void OnRadioReceiveAttempt(ref RadioReceiveAttemptEvent args)
    {
        if (ShouldCancel(args.RadioReceiver, args.Channel.Frequency))
            args.Cancelled = true;
    }

    private bool ShouldCancel(EntityUid sourceUid, int frequency)
    {
        var source = Transform(sourceUid).Coordinates;
        var query = EntityQueryEnumerator<ActiveRadioJammerComponent, RadioJammerComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out _, out var jam, out var transform))
        {
            // Check if this jammer excludes the frequency
            if (jam.FrequenciesExcluded.Contains(frequency))
                continue;

            if (_transform.InRange(source, transform.Coordinates, GetCurrentRange((uid, jam))))
            {
                return true;
            }
        }

        return false;
    }
}
