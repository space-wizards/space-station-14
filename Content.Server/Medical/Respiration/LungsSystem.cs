using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Body.Events;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Medical.Blood.Systems;
using Content.Shared.Medical.Respiration.Components;
using Content.Shared.Medical.Respiration.Events;
using Content.Shared.Medical.Respiration.Systems;

namespace Content.Server.Medical.Respiration;

public sealed class LungsSystem : SharedLungsSystem
{
    [Dependency] private AtmosphereSystem _atmosSystem = default!;

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<LungsTickingComponent, LungsComponent>();
        while (query.MoveNext(out var uid, out var lungsTickingComp, out var lungsComp))
        {
            if (GameTiming.CurTime >= lungsTickingComp.NextPhasedUpdate)
            {
                var lungs = (uid, lungsComp);
                UpdateBreathability(lungs);
                var attempt = new BreathAttemptEvent((uid, lungsComp));
                RaiseLocalEvent(uid, ref attempt);
                if (!attempt.Canceled)
                    BreathCycle(lungs);
                SetNextPhaseDelay((uid, lungsComp, lungsTickingComp));
                if (lungsComp.Phase != BreathingPhase.Suffocating)
                    AbsorbGases(lungs);
            }

            if (GameTiming.CurTime >= lungsTickingComp.NextUpdate)
            {
                var lungs = (uid, lungsComp);
                UpdateBreathability(lungs);
                AbsorbGases(lungs);
                lungsTickingComp.NextUpdate = GameTiming.CurTime + lungsTickingComp.UpdateRate;
            }
        }
    }

    /// <summary>
    /// Equalizes lung pressure, this should move air appropriately while inhaling/exhaling. This will also forcibly remove all
    /// air in the lungs when the owner is exposed to low pressure or vacuum.
    /// </summary>
    /// <param name="lungs">lung gas mixture holder component</param>
    protected override void EqualizeLungPressure(Entity<LungsComponent> lungs)
    {
        var extGas = GetBreathingAtmosphere(lungs);
        if (extGas == null)
            return;
        if (lungs.Comp.ContainedGas.Pressure > extGas.Pressure)
        {
            _atmosSystem.PumpGasTo(lungs.Comp.ContainedGas, extGas, lungs.Comp.ContainedGas.Pressure);
        }
        if (lungs.Comp.ContainedGas.Pressure <= extGas.Pressure)
        {
            _atmosSystem.PumpGasTo(extGas, lungs.Comp.ContainedGas, extGas.Pressure);
        }
        Dirty(lungs);
    }

    protected override void EmptyLungs(Entity<LungsComponent> lungs)
    {
        var externalGas = _atmosSystem.GetContainingMixture(lungs.Comp.SolutionOwnerEntity, excite: true);
        _atmosSystem.ReleaseGasTo(lungs.Comp.ContainedGas, externalGas, lungs.Comp.ContainedGas.Volume);
        base.EmptyLungs(lungs);
    }

    protected override GasMixture? GetBreathingAtmosphere(Entity<LungsComponent> lungs)
    {
        return _atmosSystem.GetContainingMixture(lungs.Comp.SolutionOwnerEntity, excite: true);
    }

}
