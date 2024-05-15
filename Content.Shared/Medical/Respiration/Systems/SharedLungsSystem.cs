using Content.Shared.Atmos;
using Content.Shared.Body.Events;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Medical.Blood.Systems;
using Content.Shared.Medical.Respiration.Components;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Medical.Respiration.Systems;


/// <summary>
///
/// </summary>
[Virtual]
public abstract class SharedLungsSystem : EntitySystem //Never forget the communal-lung incident of 2023
{
    [Dependency] protected readonly IPrototypeManager ProtoManager = default!;
    [Dependency] protected readonly IGameTiming GameTiming = default!;
    [Dependency] protected SharedSolutionContainerSystem SolutionContainerSystem = default!;
    [Dependency] protected BloodstreamSystem BloodstreamSystem = default!;
    [Dependency] protected INetManager NetManager = default!;

    private int solutionVolume = 0; //TODO: unhardcode this shit

    public override void Initialize()
    {
        solutionVolume = BloodstreamSystem.BloodstreamVolumeTEMP;
        SubscribeLocalEvent<LungsComponent, MapInitEvent>(OnLungsMapInit, after:[typeof(SharedBodySystem), typeof(BloodstreamSystem)]);
        SubscribeLocalEvent<LungsComponent, BodyInitializedEvent>(OnBodyInitialized, after: [typeof(BloodstreamSystem)]);
        base.Initialize();
    }

    private void OnBodyInitialized(EntityUid uid, LungsComponent lungsComp, ref BodyInitializedEvent args)
    {
        if (!lungsComp.UsesBodySolutions)
            return;
        SetupSolution((uid, lungsComp), args.Body, null);
        lungsComp.SolutionOwnerEntity = args.Body; //the owner is the body we are initialized in
        SetLungTicking(uid, true);
        Dirty(uid, lungsComp);
    }

    protected void BreathCycle(Entity<LungsComponent> lungs)
    {
        switch (lungs.Comp.Phase)
        {
            case BreathingPhase.Inhale:
            case BreathingPhase.Exhale:
            {
                UpdateLungVolume(lungs);
                break;
            }
            case BreathingPhase.Suffocating:
            {
                Log.Debug($"{ToPrettyString(lungs.Comp.SolutionOwnerEntity)} is suffocating!");
                return;
            }
        }
        EqualizeLungPressure(lungs);
    }

    protected void UpdateBreathability(Entity<LungsComponent> lungs)
    {
        if (HasBreathableAtmosphere(lungs, GetBreathingAtmosphere(lungs)))
        {
            if (lungs.Comp.CanBreathe)
                return;
            Log.Debug($"{ToPrettyString(lungs.Comp.SolutionOwnerEntity)} is breathing again!");
            lungs.Comp.Phase = BreathingPhase.Inhale;
            UpdateLungVolume(lungs);
            return;
        }
        if (!lungs.Comp.CanBreathe)
            return;

        Log.Debug($"{ToPrettyString(lungs.Comp.SolutionOwnerEntity)} started suffocating!");
        lungs.Comp.Phase = BreathingPhase.Suffocating;
        EmptyLungs(lungs);

    }

    private void OnLungsMapInit(EntityUid uid, LungsComponent lungsComp, ref MapInitEvent args)
    {
        if (NetManager.IsClient)
            return;

        var targetEnt = uid;
        if (!lungsComp.UsesBodySolutions)
        {
            if (!SolutionContainerSystem.EnsureSolutionEntity((uid, null),
                    lungsComp.TargetSolutionId,
                    out var solEnt))
                return; //this will only ever return false on client and map init only runs on the server.
            SetupSolution((targetEnt, lungsComp), targetEnt, solEnt);
            lungsComp.SolutionOwnerEntity = uid;//the owner is ourself
        }

        var respType = ProtoManager.Index(lungsComp.MetabolismType);
        foreach (var (gasProto, gasSettings) in respType.AbsorbedGases)
        {
            var gas = ProtoManager.Index(gasProto);
            if (gas.Reagent == null)
            {
                Log.Error($"Gas:{gas.Name} : {gas.ID} does not have an assigned reagent. This is required to be absorbable");
                continue;
            }
            lungsComp.CachedAbsorbedGasData.Add(((Gas)sbyte.Parse(gas.ID), gas.Reagent, gasSettings));
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
        lungsComp.Phase = BreathingPhase.Exhale;
        lungsComp.ContainedGas.Volume = lungsComp.NormalExhaleVolume;
        if (!lungsComp.UsesBodySolutions)
            SetLungTicking(uid, true);
        Dirty(uid, lungsComp);
    }
    private void SetupSolution(Entity<LungsComponent> lungs, EntityUid targetEnt, Entity<SolutionComponent>? solutionComp)
    {
        var targetSolEnt = solutionComp;
        if (targetSolEnt == null && !SolutionContainerSystem.TryGetSolution((targetEnt, null),
                lungs.Comp.TargetSolutionId,
                out targetSolEnt,
                out var targetSol,
                true))
            return;

        targetSol = targetSolEnt.Value.Comp.Solution;

        //set up the solution with the initial starting concentration of absorbed gases
        var metabolism = ProtoManager.Index(lungs.Comp.MetabolismType);
        foreach (var (gasId, (lowThreshold, highThreshold)) in metabolism.AbsorbedGases)
        {
            var gasProto = ProtoManager.Index(gasId);
            if (gasProto.Reagent == null)
                continue;
            targetSol.AddReagent(gasProto.Reagent, highThreshold * solutionVolume);
        }
        SolutionContainerSystem.UpdateChemicals(targetSolEnt.Value);

        //cache solutionEntities because they should never be removed
        lungs.Comp.CachedTargetSolutionEnt = targetSolEnt.Value;
        Dirty(lungs);
    }

    protected void SetNextPhaseDelay(Entity<LungsComponent, LungsTickingComponent> lungs)
    {
        lungs.Comp1.Phase = lungs.Comp1.Phase switch
        {
            BreathingPhase.Inhale => BreathingPhase.Pause,
            BreathingPhase.Pause => BreathingPhase.Exhale,
            BreathingPhase.Exhale => BreathingPhase.Inhale,
            BreathingPhase.Hold => BreathingPhase.Inhale,
            BreathingPhase.Suffocating => BreathingPhase.Suffocating,
            _ => lungs.Comp1.Phase
        };
        lungs.Comp2.NextPhasedUpdate = GameTiming.CurTime + lungs.Comp1.NextPhaseDelay;
        Dirty(lungs);
    }

    protected void UpdateLungVolume(Entity<LungsComponent> lungs)
    {
        lungs.Comp.ContainedGas.Volume = lungs.Comp.TargetVolume;
        Dirty(lungs);
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
        var extGas = GetBreathingAtmosphere((lungs, lungs.Comp));
        return HasBreathableAtmosphere(lungs, extGas);
    }

    protected virtual void EmptyLungs(Entity<LungsComponent> lungs)
    {
        lungs.Comp.ContainedGas = new() { Volume = 0 };
        Dirty(lungs);
    }

    protected void AbsorbGases(Entity<LungsComponent> lungs)
    {
        //Do not try to absorb gases if there are none there, or if there are no solution to absorb into
        if (!lungs.Comp.CanBreathe
            || lungs.Comp.ContainedGas.Volume == 0
            || lungs.Comp.CachedTargetSolutionEnt == EntityUid.Invalid
           )
            return;

        var effortMult = 0;//0 is in-range, +1 is low, -1 is high

            var targetSolEnt =
                new Entity<SolutionComponent>(lungs.Comp.CachedTargetSolutionEnt, Comp<SolutionComponent>(lungs.Comp.CachedTargetSolutionEnt));
            var targetSolution = targetSolEnt.Comp.Solution;

            foreach (var (gas, reagent, (lowConc, highConc)) in lungs.Comp.CachedAbsorbedGasData)
            {
                var gasMols = lungs.Comp.ContainedGas[(int) gas];
                if (gasMols <= 0)
                    continue;
                var reagentConc = SolutionContainerSystem.GetReagentConcentration(targetSolEnt,
                    solutionVolume,
                    new ReagentId(reagent, null));

                if (reagentConc < lowConc && effortMult <= 0)
                    effortMult = 1;
                if (reagentConc > highConc && effortMult == 0)
                {
                    effortMult = -1;
                    continue;
                }

                var concentrationDelta = highConc - reagentConc;
                if (concentrationDelta == 0)
                    continue;

                var maxAddedReagent = concentrationDelta * solutionVolume;
                var reagentToAdd = GetReagentUnitsFromMol(gasMols, reagent, lungs.Comp.ContainedGas);
                if (maxAddedReagent < reagentToAdd.Quantity)
                    reagentToAdd = new (reagentToAdd.Reagent, maxAddedReagent);
                targetSolution.AddReagent(reagentToAdd);
                lungs.Comp.ContainedGas.SetMoles(gas, gasMols-( gasMols * concentrationDelta));
            }

            foreach (var (gas, reagent, (lowConc, highConc)) in lungs.Comp.CachedWasteGasData)
            {
                var reagentConc = SolutionContainerSystem.GetReagentConcentration(targetSolEnt, solutionVolume, new ReagentId(reagent, null));
                if (reagentConc < lowConc && effortMult != 1)
                {
                    effortMult = -1;
                    continue;
                }
                if (reagentConc > highConc && effortMult == 0)
                {
                    effortMult = 1;
                }

                var reagentDelta = reagentConc - lowConc;
                if (reagentDelta == 0)
                    continue;

                var reagentToRemove = new ReagentQuantity(new (reagent, null),
                    reagentDelta * solutionVolume);
                var molsToAdd = GetMolsOfReagent(targetSolution, reagent, lungs.Comp.ContainedGas);
                var maxMolsToAdd = lungs.Comp.ContainedGas.TotalMoles * reagentDelta;
                if (maxMolsToAdd == 0)
                    continue;
                if (maxMolsToAdd < molsToAdd)
                {
                    molsToAdd = maxMolsToAdd;
                    reagentToRemove = GetReagentUnitsFromMol(molsToAdd, reagent, lungs.Comp.ContainedGas);
                }
                lungs.Comp.ContainedGas.AdjustMoles(gas, molsToAdd);
                targetSolution.RemoveReagent(reagentToRemove);
            }

            SolutionContainerSystem.UpdateChemicals(targetSolEnt, false, false);
            Dirty(lungs);

            if (effortMult == 0)
                return;
            lungs.Comp.BreathEffort =
                Math.Clamp(lungs.Comp.BreathEffort + effortMult * lungs.Comp.EffortSensitivity, 0, 1);
    }


    protected ReagentQuantity GetReagentUnitsFromMol(float gasMols, string reagentId, GasMixture gasMixture)
    {
        return new(reagentId,
            Atmospherics.MolsToVolume(gasMols,
                gasMixture.Pressure,
                gasMixture.Temperature),
            null);
    }

    protected float GetMolsOfReagent(Solution solution, string reagentId, GasMixture gasMixture)
    {
        var reagentVolume = solution.GetReagent(new (reagentId, null)).Quantity;
        return Atmospherics.VolumeToMols(reagentVolume.Float(), gasMixture.Pressure, gasMixture.Temperature);
    }

    public void SetLungTicking(Entity<LungsComponent?> lungs, bool shouldTick)
    {
        if (shouldTick)
        {
            EnsureComp<LungsTickingComponent>(lungs);
            return;
        }
        RemComp<LungsTickingComponent>(lungs);
    }


    //TODO: internals :)
    protected virtual GasMixture? GetBreathingAtmosphere(Entity<LungsComponent> lungs)
    {
        return null;
    }

    //these are stub implementations for the client, all atmos handling is handled serverside
    #region stubs

    protected virtual void EqualizeLungPressure(Entity<LungsComponent> lungs)
    {
    }
    #endregion

}
