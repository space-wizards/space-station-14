using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Body.Events;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Medical.Blood.Systems;
using Content.Shared.Medical.Respiration.Components;
using Content.Shared.Medical.Respiration.Events;
using Content.Shared.Medical.Respiration.Systems;

namespace Content.Server.Medical.Respiration;

public sealed class LungsSystem : SharedLungsSystem
{
    [Dependency] private SolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private AtmosphereSystem _atmosSystem = default!;

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
        var query = EntityQueryEnumerator<LungsComponent, LungsGasComponent, SolutionContainerManagerComponent>();
        while (query.MoveNext(out var uid, out var lungsComp, out var lungsGasComp, out var _))
        {
            if (GameTiming.CurTime >= lungsComp.NextPhasedUpdate)
            {
                UpdateBreathability(uid, lungsComp, lungsGasComp,
                    _atmosSystem.GetContainingMixture(lungsComp.SolutionOwnerEntity, excite: true));
                var attempt = new BreathAttemptEvent((uid, lungsComp));
                RaiseLocalEvent(uid, ref attempt);
                if (!attempt.Canceled)
                    BreathCycle(uid, lungsComp, lungsGasComp);
                SetNextPhaseDelay(uid, lungsComp);
            }

            if (GameTiming.CurTime >= lungsComp.NextUpdate)
            {
                UpdateBreathability(uid, lungsComp, lungsGasComp,
                    _atmosSystem.GetContainingMixture(lungsComp.SolutionOwnerEntity, excite: true));
                AbsorbGases(lungsComp, lungsGasComp);
                lungsComp.NextUpdate = GameTiming.CurTime + lungsComp.UpdateRate;
            }
        }
        base.Update(frameTime);
    }

    private void OnLungsMapInit(EntityUid uid, LungsComponent lungsComp, ref MapInitEvent args)
    {
        var gasComp = AddComp<LungsGasComponent>(uid);
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

        gasComp.ContainedGas.Volume = lungsComp.TargetLungVolume;

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

    private void SetNextPhaseDelay(EntityUid uid, LungsComponent lungsComp)
    {
        lungsComp.Phase = lungsComp.Phase switch
        {
            BreathingPhase.Hold or BreathingPhase.Pause => BreathingPhase.Inhale,
            BreathingPhase.Inhale => BreathingPhase.Exhale,
            BreathingPhase.Exhale => BreathingPhase.Pause,
            BreathingPhase.Suffocating => BreathingPhase.Suffocating,
            _ => lungsComp.Phase
        };
        lungsComp.NextPhasedUpdate = GameTiming.CurTime + lungsComp.NextPhaseDelay;
        Dirty(uid, lungsComp);
    }

    private void BreathCycle(EntityUid uid, LungsComponent lungsComp, LungsGasComponent gasComp)
    {
        var extGas = _atmosSystem.GetContainingMixture(lungsComp.SolutionOwnerEntity, excite: true);
        switch (lungsComp.Phase)
        {
            case BreathingPhase.Inhale:
            {
                UpdateLungGasVolume(gasComp, lungsComp, lungsComp.TargetLungVolume + lungsComp.TidalVolume);
                break;
            }
            case BreathingPhase.Exhale:
            {
                UpdateLungGasVolume(gasComp, lungsComp, lungsComp.TargetLungVolume - lungsComp.TidalVolume);
                break;
            }
            case BreathingPhase.Suffocating:
            {
                Log.Debug($"{ToPrettyString(lungsComp.SolutionOwnerEntity)} is suffocating!");
                return;
            }
        }
        EqualizeLungPressure(gasComp, lungsComp, extGas);
    }

    /// <summary>
    /// Equalizes lung pressure, this should move air appropriately while inhaling/exhaling. This will also forcibly remove all
    /// air in the lungs when the owner is exposed to low pressure or vacuum.
    /// </summary>
    /// <param name="gasComp">lung gas mixture holder component</param>
    /// <param name="extGas">External atmospheric gas mixture, this is null when in space</param>
    private void EqualizeLungPressure(LungsGasComponent gasComp, LungsComponent lungsComp, GasMixture? extGas)
    {
        if (extGas == null)
            return;
        if (gasComp.ContainedGas.Pressure > extGas.Pressure)
        {
            _atmosSystem.ReleaseGasTo(gasComp.ContainedGas, extGas, gasComp.ContainedGas.Pressure);
        }
        if (gasComp.ContainedGas.Pressure < extGas.Pressure)
        {
            _atmosSystem.ReleaseGasTo(extGas, gasComp.ContainedGas, extGas.Pressure);
        }
    }

    private void AbsorbGases(LungsComponent lungsComp, LungsGasComponent gasComp)
    {
        //Do not try to absorb gases if there are none there
        if (lungsComp.CanBreathe || gasComp.ContainedGas.Volume == 0)
            return;

        var scalingFactor = 1 / (float)lungsComp.NextUpdate.TotalSeconds;

        var absorbSolEnt =
            new Entity<SolutionComponent>(lungsComp.CachedAbsorptionSolutionEnt, Comp<SolutionComponent>(lungsComp.CachedAbsorptionSolutionEnt));
        var wasteSolEnt =
            new Entity<SolutionComponent>(lungsComp.CachedWasteSolutionEnt, Comp<SolutionComponent>(lungsComp.CachedWasteSolutionEnt));
        var absorbedSolution = absorbSolEnt.Comp.Solution;
        var wasteSolution = wasteSolEnt.Comp.Solution;

        foreach (var (gas, reagent, maxAbsorption) in lungsComp.CachedAbsorbedGasData)
        {
            var oldGasMols = gasComp.ContainedGas[(int) gas];
            if (oldGasMols <= 0)
                continue;

            //factor in the timescale so that the max absorption rate is always per second.
            var adjustedMaxAbsorption = maxAbsorption * scalingFactor;

            var reagentSaturation = absorbedSolution.GetReagent(new ReagentId(reagent, null)).Quantity.Float()/absorbedSolution.Volume.Float();
            if (reagentSaturation >= adjustedMaxAbsorption)
                continue;
            var absorptionPercentage = adjustedMaxAbsorption - reagentSaturation;
            var gasMols = oldGasMols* absorptionPercentage;
            absorbedSolution.AddReagent(GetReagentUnitsFromMol(gasMols, reagent));
            gasComp.ContainedGas.SetMoles(gas, oldGasMols-gasMols);
        }

        foreach (var (gas, reagent, maxRelease) in lungsComp.CachedWasteGasData)
        {
            var oldGasMols = gasComp.ContainedGas[(int) gas];
            var adjustedMaxRelease = maxRelease * scalingFactor;

            //make sure we calculate the max concentration to release into the lungs
            var wasteConcentration = wasteSolution.GetReagent(new(reagent, null)).Quantity.Float() / wasteSolution.Volume.Float();
            wasteConcentration = MathF.Min(wasteConcentration, adjustedMaxRelease);
            var gasMolCreated = GetMolsOfReagent(wasteSolution, reagent) * wasteConcentration - oldGasMols;
            if (gasMolCreated <= 0)
                continue;

            gasComp.ContainedGas.AdjustMoles(gas, gasMolCreated);
            wasteSolution.RemoveReagent(GetReagentUnitsFromMol(gasMolCreated, reagent));
        }

        _solutionContainerSystem.UpdateChemicals(absorbSolEnt);
        _solutionContainerSystem.UpdateChemicals(wasteSolEnt);
    }

    private void UpdateBreathability(EntityUid uid, LungsComponent lungsComp, LungsGasComponent gasComp, GasMixture? extGas)
    {
        var breathable = HasBreathableAtmosphere(uid, extGas);
        if (breathable && lungsComp.CanBreathe || !breathable && !lungsComp.CanBreathe)
            return; //no updating needed
        if (breathable)
        {
            Log.Debug($"{ToPrettyString(lungsComp.SolutionOwnerEntity)} is breathing again!");
            lungsComp.Phase = BreathingPhase.Inhale;
            UpdateLungGasVolume(gasComp, lungsComp, lungsComp.TargetLungVolume + lungsComp.TidalVolume, true);
            return;
        }
        Log.Debug($"{ToPrettyString(lungsComp.SolutionOwnerEntity)} started suffocating!");
        lungsComp.Phase = BreathingPhase.Suffocating;
        EmptyLungs(gasComp, extGas);
        UpdateLungGasVolume(gasComp, lungsComp, 0, true);

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


    private void EmptyLungs(LungsGasComponent gasComp, GasMixture? externalGas)
    {
        _atmosSystem.ReleaseGasTo(gasComp.ContainedGas, externalGas, gasComp.ContainedGas.Volume);
        gasComp.ContainedGas = new();
    }

    private ReagentQuantity GetReagentUnitsFromMol(float gasMols, string reagentId)
    {
        return new(reagentId, Shared.Chemistry.Constants.LiquidRUFromMoles(gasMols), null);
    }


    private float GetMolsOfReagent(Solution solution, string reagentId)
    {
        var reagentAmount = solution.GetReagent(new (reagentId, null)).Quantity;
        return Shared.Chemistry.Constants.LiquidMolesFromRU(reagentAmount);
    }

    private void UpdateLungGasVolume(LungsGasComponent gasComp,LungsComponent lungsComp, float newVolume, bool force = false)
    {
        if (force)
        {
            gasComp.ContainedGas.Volume = MathF.Min(newVolume, lungsComp.TotalVolume);
            return;
        }
        gasComp.ContainedGas.Volume = Math.Clamp(newVolume, lungsComp.ResidualVolume, lungsComp.TotalVolume);
    }


}
