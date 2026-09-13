using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.Timing;

namespace Content.Shared.Trigger.Systems;

/// <summary>
/// Releases a gas mixture to the atmosphere when triggered.
/// Can also release gas over a set timespan to prevent trolling people
/// with the instant-wall-of-pressure-inator.
/// </summary>
public abstract partial class SharedReleaseGasOnTriggerSystem : XOnTriggerSystem<ReleaseGasOnTriggerComponent>
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private IGameTiming _timing = default!;

    /// <summary>
    /// Shrimply sets the component to active when triggered, allowing it to release over time.
    /// </summary>
    protected override void OnTrigger(Entity<ReleaseGasOnTriggerComponent> ent, EntityUid _, ref TriggerEvent args)
    {
        ent.Comp.Active = true;
        ent.Comp.NextReleaseTime = _timing.CurTime;
        ent.Comp.StartingTotalMoles = ent.Comp.Air.TotalMoles;
        _appearance.SetData(ent, ReleaseGasOnTriggerVisuals.Key, true);
        args.Handled = true;
    }
}
