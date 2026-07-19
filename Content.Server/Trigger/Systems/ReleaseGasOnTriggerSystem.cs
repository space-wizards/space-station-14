using Content.Server.Atmos.EntitySystems;
using Content.Shared.Trigger.Components.Effects;
using Content.Shared.Trigger.Systems;
using Robust.Shared.Timing;

namespace Content.Server.Trigger.Systems;

public sealed partial class ReleaseGasOnTriggerSystem : SharedReleaseGasOnTriggerSystem
{
    [Dependency] private AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ReleaseGasOnTriggerComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ReleaseGasOnTriggerComponent, EntityTimerEvent>(OnTimer);
    }

    private void OnStartup(Entity<ReleaseGasOnTriggerComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.Active)
            Timers.SetTimerAt(ent, ReleaseTimer, ent.Comp.NextReleaseTime);
    }

    private void OnTimer(Entity<ReleaseGasOnTriggerComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id != ReleaseTimer || !ent.Comp.Active)
            return;

        var comp = ent.Comp;
        var giverGasMix = comp.Air.Remove(comp.StartingTotalMoles * comp.RemoveFraction);
        var environment = _atmosphereSystem.GetContainingMixture(ent.Owner, false, true);

        if (environment == null)
        {
            _appearance.SetData(ent, ReleaseGasOnTriggerVisuals.Key, false);
            RemCompDeferred<ReleaseGasOnTriggerComponent>(ent);
            return;
        }

        _atmosphereSystem.Merge(environment, giverGasMix);
        comp.NextReleaseTime = args.ScheduledTime + comp.ReleaseInterval;
        Timers.SetTimerAt(ent, ReleaseTimer, comp.NextReleaseTime);

        if (comp.PressureLimit != 0 && environment.Pressure >= comp.PressureLimit ||
            comp.Air.TotalMoles <= 0)
        {
            _appearance.SetData(ent, ReleaseGasOnTriggerVisuals.Key, false);
            RemCompDeferred<ReleaseGasOnTriggerComponent>(ent);
        }
    }
}
