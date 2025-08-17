using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.Timing;

namespace Content.Shared.Trigger.Systems;

/// <summary>
/// Releases a gas mixture to the atmosphere when triggered.
/// Can also release gas over a set timespan to prevent trolling people
/// with the instant-wall-of-pressure-inator.
/// </summary>
public abstract class SharedReleaseGasOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReleaseGasOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    /// <summary>
    /// Shrimply sets the component to active when triggered, allowing it to release over time.
    /// </summary>
    private void OnTrigger(Entity<ReleaseGasOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        ent.Comp.Active = true;
        ent.Comp.NextReleaseTime = _timing.CurTime;
        ent.Comp.StartingTotalMoles = ent.Comp.Air.TotalMoles;
        _appearance.SetData(ent, ReleaseGasOnTriggerVisuals.Key, true);
    }
}
