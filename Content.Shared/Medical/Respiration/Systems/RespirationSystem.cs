using Content.Shared.Atmos;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Medical.Respiration.Components;
using Content.Shared.Medical.Respiration.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Medical.Respiration.Systems;

/// <summary>
/// This handles...
/// </summary>
public sealed class RespirationSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionSystem = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<RespiratorComponent, MapInitEvent>(OnRespiratorMapInit);
    }
    private void OnRespiratorMapInit(EntityUid uid, RespiratorComponent respirator, ref MapInitEvent args)
    {
        var targetEnt = uid;
        if (respirator.GetSolutionsFromEvent)
        {
            var ev = new GetRespiratorTargetSolutionEvent((uid, respirator));
            RaiseLocalEvent(uid, ref ev);
            if (ev.Target != null)
                targetEnt = ev.Target.Value;
        }

        if (!_solutionSystem.TryGetSolution((targetEnt, null), respirator.AbsorbOutputSolution,
                out var absorbedSolEnt, out var absorbedSol, true)
            || !_solutionSystem.TryGetSolution((targetEnt, null), respirator.WasteSourceSolution,
                out var wasteSolEnt, out var wasteSol, true))
            return;

        //cache all the things!
        respirator.CachedAbsorptionSolutionEnt = absorbedSolEnt.Value;
        respirator.CachedWasteSolutionEnt = wasteSolEnt.Value;
        var respType = _protoManager.Index(respirator.RespirationType);
        foreach (var (gasProto, maxAbsorption) in respType.AbsorbedGases)
        {
            var gas = _protoManager.Index(gasProto);
            respirator.CachedAbsorbedGasData.Add(((Gas)sbyte.Parse(gas.ID), gas.Reagent, maxAbsorption));
        }
        foreach (var (gasProto, maxAbsorption) in respType.WasteGases)
        {
            var gas = _protoManager.Index(gasProto);
            respirator.CachedWasteGasData.Add(((Gas)sbyte.Parse(gas.ID), gas.Reagent, maxAbsorption));
        }
        UpdateSolutions((uid, respirator), absorbedSolEnt.Value, wasteSolEnt.Value);
        Dirty(uid, respirator);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<RespiratorComponent, SolutionContainerManagerComponent>();
        while (query.MoveNext(out var uid, out var respiratorComp, out var _))
        {
            if (_gameTiming.CurTime < respiratorComp.NextUpdate)
                continue;
            respiratorComp.NextUpdate += respiratorComp.CycleRate;
            var respirator = (uid, respiratorComp);
            var attempt = new BreathAttemptEvent(respirator);
            RaiseLocalEvent(uid, ref attempt);
            if (attempt.Canceled)
                return;

            UpdateSolutions(respirator,
                (respiratorComp.CachedAbsorptionSolutionEnt,
                    Comp<SolutionComponent>(respiratorComp.CachedAbsorptionSolutionEnt)),
                (respiratorComp.CachedWasteSolutionEnt,
                    Comp<SolutionComponent>(respiratorComp.CachedWasteSolutionEnt)));
        }

    }


    private void UpdateSolutions(Entity<RespiratorComponent> respirator,
        Entity<SolutionComponent> absorbSolEnt, Entity<SolutionComponent> wasteSolEnt)
    {
        var gasToRemove = new List<GasAmount>();
        var gasToAdd = new List<GasAmount>();
        foreach (var (gas, reagent, maxAbsorption) in respirator.Comp.CachedAbsorbedGasData)
        {
            if (reagent == null || maxAbsorption == 0)
                continue;
            AbsorbGases(respirator, absorbSolEnt.Comp.Solution, gas, new(reagent, null), maxAbsorption, ref gasToAdd);
        }
        foreach (var (gas, reagent, maxAbsorption) in respirator.Comp.CachedWasteGasData)
        {
            if (reagent == null || maxAbsorption == 0)
                continue;
            VentGases(respirator, absorbSolEnt.Comp.Solution, gas, new(reagent, null), maxAbsorption, ref gasToRemove);
        }

        UpdateGases(respirator, gasToAdd, gasToRemove);
        _solutionSystem.UpdateChemicals(absorbSolEnt);
        Dirty(respirator);
        var ev = new BreatheEvent(respirator, absorbSolEnt, absorbSolEnt.Comp.Solution,
            wasteSolEnt, wasteSolEnt.Comp.Solution);
        RaiseLocalEvent(respirator, ref ev);
    }

    private void AbsorbGases(Entity<RespiratorComponent> respirator,
        Solution solution, Gas gas, ReagentId reagent, float maxAbsorption, ref List<GasAmount> gasToRemove)
    {
        foreach (var gasAmt in respirator.Comp.ContainedGases.Gases)
        {
            if (gasAmt.Gas != gas)
                continue;

            var reagentUnits = Chemistry.Constants.RUFromMoles(maxAbsorption * gasAmt.Mols);
            var difference = reagentUnits - solution.GetReagentQuantity(reagent);
            if (difference <= 0)
                break;

            gasToRemove.Add(new GasAmount(gas, maxAbsorption * gasAmt.Mols));
            solution.AddReagent(reagent, reagentUnits);
            break;
        }
    }

    private void VentGases(Entity<RespiratorComponent> respirator,
        Solution solution, Gas gas, ReagentId reagent, float maxAbsorption, ref List<GasAmount> gasToRemove)
    {
        foreach (var gasAmt in respirator.Comp.ContainedGases.Gases)
        {
            if (gasAmt.Gas != gas)
                continue;

            var reagentUnits = Chemistry.Constants.RUFromMoles(maxAbsorption * gasAmt.Mols);
            var difference = solution.GetReagentQuantity(reagent) - reagentUnits;
            if (difference <= 0)
                break;

            gasToRemove.Add(new GasAmount(gas, maxAbsorption * gasAmt.Mols));
            solution.RemoveReagent(reagent, reagentUnits);
            break;
        }
    }

    private void UpdateGases(Entity<RespiratorComponent> respirator, List<GasAmount> gasToAdd, List<GasAmount> gasToRemove)
    {
        foreach (var gasAmt in gasToAdd)
        {
            respirator.Comp.ContainedGases.AddGasAmount(gasAmt);
        }
        foreach (var gasAmt in gasToRemove)
        {
            respirator.Comp.ContainedGases.RemoveGasAmount(gasAmt);
        }
    }
}
