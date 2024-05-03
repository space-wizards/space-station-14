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
    [Dependency] private SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private AtmosphereSystem _atmosSystem = default!;
    [Dependency] private BloodstreamSystem _bloodstream = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<LungsComponent, MapInitEvent>(OnLungsMapInit, after:[typeof(BodySystem), typeof(BloodstreamSystem)]);
        SubscribeLocalEvent<LungsComponent, BodyInitializedEvent>(OnBodyInitialized, after: [typeof(BloodstreamSystem)]);

    }

    private void OnBodyInitialized(EntityUid uid, LungsComponent lungsComp, ref BodyInitializedEvent args)
    {
        if (!lungsComp.UsesBodySolutions)
            return;
        CacheSolutionEntities((uid, lungsComp), args.Body);
        lungsComp.SolutionOwnerEntity = args.Body; //the owner is the body we are initialized in
        Dirty(uid, lungsComp);

    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<LungsComponent, SolutionContainerManagerComponent>();
        while (query.MoveNext(out var uid, out var lungsComp, out var _))
        {
            if (GameTiming.CurTime >= lungsComp.NextPhasedUpdate)
            {
                var lungs = (uid, lungsComp);
                UpdateBreathability(lungs,
                    _atmosSystem.GetContainingMixture(lungsComp.SolutionOwnerEntity, excite: true));
                var attempt = new BreathAttemptEvent((uid, lungsComp));
                RaiseLocalEvent(uid, ref attempt);
                if (!attempt.Canceled)
                    BreathCycle(lungs);
                SetNextPhaseDelay(lungs);
            }

            if (GameTiming.CurTime >= lungsComp.NextUpdate)
            {
                var lungs = (uid, lungsComp);
                UpdateBreathability(lungs,
                    _atmosSystem.GetContainingMixture(lungsComp.SolutionOwnerEntity, excite: true));
                AbsorbGases(lungs);
                lungsComp.NextUpdate = GameTiming.CurTime + lungsComp.UpdateRate;
            }
        }
        base.Update(frameTime);
    }

    private void OnLungsMapInit(EntityUid uid, LungsComponent lungsComp, ref MapInitEvent args)
    {
        var targetEnt = uid;

        if (!lungsComp.UsesBodySolutions)
        {
            CacheSolutionEntities((targetEnt, lungsComp), targetEnt);
            lungsComp.SolutionOwnerEntity = uid;//the owner is ourself
        }

        var respType = ProtoManager.Index(lungsComp.RespirationType);
        foreach (var (gasProto, maxAbsorption) in respType.AbsorbedGases)
        {
            var gas = ProtoManager.Index(gasProto);
            if (gas.Reagent == null)
            {
                Log.Error($"Gas:{gas.Name} : {gas.ID} does not have an assigned reagent. This is required to be absorbable");
                continue;
            }
            lungsComp.CachedAbsorbedGasData.Add(((Gas)sbyte.Parse(gas.ID), gas.Reagent, maxAbsorption));
        }
        foreach (var (gasProto, maxAbsorption) in respType.WasteGases)
        {
            var gas = ProtoManager.Index(gasProto);
            if (gas.Reagent == null)
            {
                Log.Error($"Gas:{gas.Name} : {gas.ID} does not have an assigned reagent. This is required to be absorbable");
                continue;
            }
            lungsComp.CachedWasteGasData.Add(((Gas)sbyte.Parse(gas.ID), gas.Reagent, maxAbsorption));
        }

        lungsComp.ContainedGas.Volume = lungsComp.TargetLungVolume;

        // //setup initial contained gas, so you immediately don't start suffocating
        // //TODO: override this when using internals so that vox don't eat shit when they spawn
        // var extGas = _atmosSystem.GetContainingMixture(lungsComp.SolutionOwnerEntity, excite: true);
        // if (extGas != null)
        // {
        //     //There is probably a better way to do this but fuck it
        //     _atmosSystem.ReleaseGasTo(extGas, gasComp.ContainedGas, extGas.Pressure);
        // }
        Dirty(uid, lungsComp);
    }


    private void CacheSolutionEntities(Entity<LungsComponent> lungs, EntityUid targetEnt)
    {
        if (!_solutionContainerSystem.TryGetSolution((targetEnt, null), lungs.Comp.AbsorbOutputSolution,
                out var absorbedSolEnt, out var absorbedSol, true)
            || !_solutionContainerSystem.TryGetSolution((targetEnt, null), lungs.Comp.WasteSourceSolution,
                out var wasteSolEnt, out var wasteSol, true))
            return;
        //cache all the things!
        lungs.Comp.CachedAbsorptionSolutionEnt = absorbedSolEnt.Value;
        lungs.Comp.CachedWasteSolutionEnt = wasteSolEnt.Value;
        Dirty(lungs);
    }

    private void SetNextPhaseDelay(Entity<LungsComponent> lungs)
    {
        lungs.Comp.Phase = lungs.Comp.Phase switch
        {
            BreathingPhase.Hold or BreathingPhase.Pause => BreathingPhase.Inhale,
            BreathingPhase.Inhale => BreathingPhase.Exhale,
            BreathingPhase.Exhale => BreathingPhase.Pause,
            BreathingPhase.Suffocating => BreathingPhase.Suffocating,
            _ => lungs.Comp.Phase
        };
        lungs.Comp.NextPhasedUpdate = GameTiming.CurTime + lungs.Comp.NextPhaseDelay;
        Dirty(lungs);
    }

    private void BreathCycle(Entity<LungsComponent> lungs)
    {
        var extGas = _atmosSystem.GetContainingMixture(lungs.Comp.SolutionOwnerEntity, excite: true);
        switch (lungs.Comp.Phase)
        {
            case BreathingPhase.Inhale:
            {
                UpdateLungGasVolume(lungs, lungs.Comp.TargetLungVolume + lungs.Comp.TidalVolume);
                break;
            }
            case BreathingPhase.Exhale:
            {
                UpdateLungGasVolume(lungs, lungs.Comp.TargetLungVolume - lungs.Comp.TidalVolume);
                break;
            }
            case BreathingPhase.Suffocating:
            {
                Log.Debug($"{ToPrettyString(lungs.Comp.SolutionOwnerEntity)} is suffocating!");
                return;
            }
        }
        EqualizeLungPressure(lungs, extGas);
    }

    /// <summary>
    /// Equalizes lung pressure, this should move air appropriately while inhaling/exhaling. This will also forcibly remove all
    /// air in the lungs when the owner is exposed to low pressure or vacuum.
    /// </summary>
    /// <param name="gasComp">lung gas mixture holder component</param>
    /// <param name="extGas">External atmospheric gas mixture, this is null when in space</param>
    private void EqualizeLungPressure(Entity<LungsComponent> lungs, GasMixture? extGas)
    {
        if (extGas == null)
            return;
        if (lungs.Comp.ContainedGas.Pressure > extGas.Pressure)
        {
            _atmosSystem.ReleaseGasTo(lungs.Comp.ContainedGas, extGas, lungs.Comp.ContainedGas.Pressure);
        }
        if (lungs.Comp.ContainedGas.Pressure < extGas.Pressure)
        {
            _atmosSystem.ReleaseGasTo(extGas, lungs.Comp.ContainedGas, extGas.Pressure);
        }
        Dirty(lungs);
    }

    private void AbsorbGases(Entity<LungsComponent> lungs)
    {
        //Do not try to absorb gases if there are none there
        if (!lungs.Comp.CanBreathe || lungs.Comp.ContainedGas.Volume == 0)
            return;
        var scalingFactor = 1;

        var absorbSolEnt =
            new Entity<SolutionComponent>(lungs.Comp.CachedAbsorptionSolutionEnt, Comp<SolutionComponent>(lungs.Comp.CachedAbsorptionSolutionEnt));
        var wasteSolEnt =
            new Entity<SolutionComponent>(lungs.Comp.CachedWasteSolutionEnt, Comp<SolutionComponent>(lungs.Comp.CachedWasteSolutionEnt));
        var absorbedSolution = absorbSolEnt.Comp.Solution;
        var wasteSolution = wasteSolEnt.Comp.Solution;

        foreach (var (gas, reagent, maxAbsorption) in lungs.Comp.CachedAbsorbedGasData)
        {
            var oldGasMols = lungs.Comp.ContainedGas[(int) gas];
            if (oldGasMols <= 0)
                continue;

            //factor in the timescale so that the max absorption rate is always per second.
            var adjustedMaxAbsorption = maxAbsorption * scalingFactor;

            var reagentSaturation = _solutionContainerSystem.GetReagentConcentration(absorbSolEnt, 250, new ReagentId(reagent, null));
            if (reagentSaturation >= adjustedMaxAbsorption)
                continue; //TODO: rewrite this so that max blood concentration will never exceed gas concentration
            var absorptionPercentage = adjustedMaxAbsorption - reagentSaturation;
            var gasMols = oldGasMols* absorptionPercentage;
            absorbedSolution.AddReagent(GetReagentUnitsFromMol(gasMols, lungs.Comp.ContainedGas.Pressure,
                lungs.Comp.ContainedGas.Temperature, reagent));
            lungs.Comp.ContainedGas.SetMoles(gas, oldGasMols-gasMols);
        }

        foreach (var (gas, reagent, maxRelease) in lungs.Comp.CachedWasteGasData)
        {
            var oldGasMols = lungs.Comp.ContainedGas[(int) gas];
            var adjustedMaxRelease = maxRelease * scalingFactor;

            if (wasteSolution.Volume <= 0)
                continue;

            //make sure we calculate the max concentration to release into the lungs
            var wasteConcentration = _solutionContainerSystem.GetReagentConcentration(wasteSolEnt, 250, new ReagentId(reagent, null));
            wasteConcentration = MathF.Min(wasteConcentration, adjustedMaxRelease);
            if (wasteConcentration <= 0)
                return;
            var gasMolCreated = GetMolsOfReagent(wasteSolution, reagent, lungs.Comp.ContainedGas.Pressure,
                lungs.Comp.ContainedGas.Temperature) * wasteConcentration - oldGasMols;
            if (gasMolCreated <= 0)
                continue;

            lungs.Comp.ContainedGas.AdjustMoles(gas, gasMolCreated);
            wasteSolution.RemoveReagent(GetReagentUnitsFromMol(gasMolCreated, lungs.Comp.ContainedGas.Pressure,
                lungs.Comp.ContainedGas.Temperature, reagent));
        }

        _solutionContainerSystem.UpdateChemicals(absorbSolEnt);
        _solutionContainerSystem.UpdateChemicals(wasteSolEnt);

        Dirty(lungs);
    }

    private void UpdateBreathability(Entity<LungsComponent> lungs, GasMixture? extGas)
    {
        var breathable = HasBreathableAtmosphere(lungs, extGas);
        if (breathable && lungs.Comp.CanBreathe || !breathable && !lungs.Comp.CanBreathe)
            return; //no updating needed
        if (breathable)
        {
            Log.Debug($"{ToPrettyString(lungs.Comp.SolutionOwnerEntity)} is breathing again!");
            lungs.Comp.Phase = BreathingPhase.Inhale;
            UpdateLungGasVolume(lungs, lungs.Comp.TargetLungVolume + lungs.Comp.TidalVolume, true);
            return;
        }
        Log.Debug($"{ToPrettyString(lungs.Comp.SolutionOwnerEntity)} started suffocating!");
        lungs.Comp.Phase = BreathingPhase.Suffocating;
        EmptyLungs(lungs, extGas);
        UpdateLungGasVolume(lungs, 0, true);

    }

    private bool IsBreathableGasMix(GasMixture? gasMixture)
    {
        return gasMixture != null && gasMixture.Pressure >= Atmospherics.HazardLowPressure;
    }

    //TODO: internals :)
    public bool HasBreathableAtmosphere(EntityUid uid, GasMixture? gasMixture)
    {
        return IsBreathableGasMix(gasMixture);
    }


    public bool HasBreathableAtmosphere(Entity<LungsComponent?> lungs)
    {
        if (!Resolve(lungs, ref lungs.Comp))
            return true; //if we have no lungs anything is breathable
        var extGas = _atmosSystem.GetContainingMixture(lungs.Comp.SolutionOwnerEntity, excite: true);
        return HasBreathableAtmosphere(lungs, extGas);
    }


    private void EmptyLungs(Entity<LungsComponent> lungs, GasMixture? externalGas)
    {
        _atmosSystem.ReleaseGasTo(lungs.Comp.ContainedGas, externalGas, lungs.Comp.ContainedGas.Volume);
        lungs.Comp.ContainedGas = new();
    }

    private ReagentQuantity GetReagentUnitsFromMol(float gasMols, float pressure, float temp, string reagentId)
    {
        return new(reagentId, Atmospherics.MolsToVolume(gasMols, pressure, temp), null);
    }


    private float GetMolsOfReagent(Solution solution, string reagentId, float pressure, float temp)
    {
        var reagentVolume = solution.GetReagent(new (reagentId, null)).Quantity;
        return Atmospherics.VolumeToMols(reagentVolume.Float(), pressure, temp);
    }

    private void UpdateLungGasVolume(Entity<LungsComponent> lungs, float newVolume, bool force = false)
    {
        if (force)
        {
            lungs.Comp.ContainedGas.Volume = MathF.Min(newVolume, lungs.Comp.TotalVolume);
            return;
        }
        lungs.Comp.ContainedGas.Volume = Math.Clamp(newVolume, lungs.Comp.ResidualVolume, lungs.Comp.TotalVolume);
        Dirty(lungs);
    }


}
