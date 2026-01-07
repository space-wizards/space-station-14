using Content.Shared.Beeper.Components;
using Content.Shared.ProximityDetection;

namespace Content.Shared.Beeper.Systems;

/// <summary>
/// This handles controlling a beeper from proximity detector events.
/// </summary>
public sealed class ProximityBeeperSystem : EntitySystem
{
    [Dependency] private readonly BeeperSystem _beeper = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ProximityBeeperComponent, NewProximityTargetEvent>(OnNewProximityTarget);
        SubscribeLocalEvent<ProximityBeeperComponent, ProximityTargetUpdatedEvent>(OnProximityTargetUpdate);
    }

    private void OnProximityTargetUpdate(Entity<ProximityBeeperComponent> ent, ref ProximityTargetUpdatedEvent args)
    {
        _beeper.SetIntervalScaling(ent.Owner, args.Distance / args.Detector.Comp.Range);
    }

    private void OnNewProximityTarget(Entity<ProximityBeeperComponent> ent, ref NewProximityTargetEvent args)
    {
        _beeper.SetMute(ent.Owner, args.Target == null);
    }
}
