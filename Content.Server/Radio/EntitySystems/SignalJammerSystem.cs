using Content.Server.Power.Components;
using Content.Server.Radio.Components;
using Content.Shared.Examine;
using Content.Shared.Radio.Components;

namespace Content.Server.Radio.EntitySystems;

/// <summary>
/// Handles the signal jammer array, which is a stationary device that jams radio communications
/// within a large radius when anchored and powered.
/// </summary>
public sealed class SignalJammerSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SignalJammerComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<RadioSendAttemptEvent>(OnRadioSendAttempt);
    }

    private void OnExamine(EntityUid uid, SignalJammerComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var isActive = IsJammerActive(uid, component);
        var stateText = isActive ? "active" : "inactive";

        args.PushMarkup(
            Loc.GetString(
                "signal-jammer-examine",
                ("state", stateText),
                ("range", component.Range))
        );
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<SignalJammerComponent, ApcPowerReceiverComponent, TransformComponent>();

        while (query.MoveNext(out var entity, out var jammer, out var powerReceiver, out var transform))
        {
            // Only work when anchored
            if (!transform.Anchored)
            {
                RemCompDeferred<ActiveRadioJammerComponent>(entity);
                continue;
            }

            // Check if we're receiving power
            var receivingPower = powerReceiver.Powered;

            if (receivingPower)
            {
                // Ensure we have the active component
                EnsureComp<ActiveRadioJammerComponent>(entity);
            }
            else
            {
                // No power, deactivate
                RemCompDeferred<ActiveRadioJammerComponent>(entity);
            }
        }
    }

    private void OnRadioSendAttempt(ref RadioSendAttemptEvent args)
    {
        if (ShouldCancelSend(args.RadioSource, args.Channel.Frequency))
        {
            args.Cancelled = true;
        }
    }

    private bool ShouldCancelSend(EntityUid sourceUid, int frequency)
    {
        var source = Transform(sourceUid).Coordinates;
        var query = EntityQueryEnumerator<ActiveRadioJammerComponent, SignalJammerComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out _, out var jammer, out var transform))
        {
            // Check if anchored (double check)
            if (!transform.Anchored)
                continue;

            // Check if in range
            if (_transform.InRange(source, transform.Coordinates, jammer.Range))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsJammerActive(EntityUid uid, SignalJammerComponent component)
    {
        if (!TryComp<TransformComponent>(uid, out var transform))
            return false;

        if (!transform.Anchored)
            return false;

        if (!TryComp<ApcPowerReceiverComponent>(uid, out var powerReceiver))
            return false;

        return powerReceiver.Powered;
    }
}
