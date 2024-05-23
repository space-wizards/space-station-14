using Content.Shared.Body.Events;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Medical.Metabolism.Components;

namespace Content.Shared.Medical.Metabolism.Systems;

public sealed partial class MetabolismSystem
{
    private void MetabolizerInit()
    {
        SubscribeLocalEvent<MetabolizerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MetabolizerComponent, BodyInitializedEvent>(OnBodyInit);
        SubscribeLocalEvent<MetabolizerComponent, ComponentInit>(OnCompInit);
    }

    //TODO: differ starting metabolizer ticking until after body initializes fully
    private void MetabolizerUpdate(float frameTime)
    {
        var query = EntityQueryEnumerator<MetabolizerComponent>();
        while (query.MoveNext(out var uid, out var metabolism))
        {
            if (_gameTiming.CurTime < metabolism.NextUpdate)
                continue;

            metabolism.NextUpdate += metabolism.UpdateInterval;
            if (!DoMetabolicReaction((uid,metabolism)))
            {
                _damageSystem.TryChangeDamage(uid,
                    metabolism.CachedDeprivationDamage,
                    true,
                    false);
                //TODO: block entity healing
            }
        }
    }

    private void OnCompInit(EntityUid uid, MetabolizerComponent metabolizer, ComponentInit args)
    {
        var metabolismType = _protoManager.Index(metabolizer.MetabolismType);

        metabolizer.CachedDeprivationDamage = metabolismType.DeprivationDamage;
        metabolizer.CachedKCalPerReagent = metabolismType.KCalPerEnergyReagent;
        if (metabolismType.EnergyReagent != null)
            metabolizer.CachedEnergyReagent = new ReagentId(metabolismType.EnergyReagent.Value, null);

        //prevent accidents, yaml should not be setting values in these but just in case...
        metabolizer.CachedAbsorbedReagents.Clear();
        metabolizer.CachedWasteReagents.Clear();
        foreach (var (reagentId, minAmt) in metabolismType.RequiredReagents)
        {
            metabolizer.CachedAbsorbedReagents.Add(new ReagentQuantity(reagentId, minAmt, null));
        }
        foreach (var (reagentId, minAmt) in metabolismType.WasteReagents)
        {
            metabolizer.CachedWasteReagents.Add(new ReagentQuantity(reagentId, minAmt, null));
        }
        Dirty(uid, metabolizer);
    }

    private void OnMapInit(EntityUid uid, MetabolizerComponent metabolizer, MapInitEvent args)
    {
        if (_netManager.IsClient)
            return;
        if (metabolizer.UsesBodySolution) //if we are using a solution on body, differ initializing cached solutions
            return;

        UpdateCachedMetabolizerSolutions((uid, metabolizer), uid);
    }

    private void OnBodyInit(EntityUid uid, MetabolizerComponent metabolizer, BodyInitializedEvent args)
    {
        if (!metabolizer.UsesBodySolution)
            return;
        UpdateCachedMetabolizerSolutions((uid, metabolizer), args.Body);
    }


    private bool DoMetabolicReaction(Entity<MetabolizerComponent> metabolizer)
    {
        if (metabolizer.Comp.CachedAbsorbSolutionEnt == EntityUid.Invalid
            || metabolizer.Comp.CachedWasteSolutionEnt == EntityUid.Invalid)
            return false;
        var absorbSol = Comp<SolutionComponent>(metabolizer.Comp.CachedAbsorbSolutionEnt);
        var wasteSol = Comp<SolutionComponent>(metabolizer.Comp.CachedWasteSolutionEnt);


        //TODO: some of this can be simplified if/when there is a common unit reactions calculation method in chem
        foreach (var (reagentId, quantity) in metabolizer.Comp.CachedAbsorbedReagents)
        {
            var reqQuant = quantity * metabolizer.Comp.BaseMultiplier;
            if (!absorbSol.Solution.TryGetReagentQuantity(reagentId, out var solQuant) || solQuant < reqQuant)
            {
                return false;
            }
        }

        //actually remove the reagents now
        foreach (var (reagentId, quantity) in metabolizer.Comp.CachedAbsorbedReagents)
        {
            var reqQuant = quantity*metabolizer.Comp.BaseMultiplier;
            absorbSol.Solution.RemoveReagent(reagentId, reqQuant);
        }
        Dirty(metabolizer.Comp.CachedAbsorbSolutionEnt, absorbSol);

        if (metabolizer.Comp.CachedWasteReagents.Count == 0)
            return true; //if there are no waste products then exit out
        foreach (var (reagentId, quantity) in metabolizer.Comp.CachedWasteReagents)
        {
            var reqQuant = quantity*metabolizer.Comp.BaseMultiplier;
            absorbSol.Solution.AddReagent(reagentId, reqQuant);
        }
        _solutionContainerSystem.UpdateChemicals((metabolizer.Comp.CachedWasteSolutionEnt,wasteSol));
        return true;
    }

    private void UpdateCachedMetabolizerSolutions(Entity<MetabolizerComponent> metabolizer,
        EntityUid? target)
    {
        target ??= metabolizer.Owner;

        var dirty = false;
        if (_solutionContainerSystem.TryGetSolution((target.Value, null), metabolizer.Comp.AbsorbSolutionId, out var absorbSol, true))
        {
            metabolizer.Comp.CachedAbsorbSolutionEnt = absorbSol.Value.Owner;
            dirty = true;
        }
        if (_solutionContainerSystem.TryGetSolution((target.Value, null), metabolizer.Comp.WasteSolutionId, out var wasteSol, true))
        {
            metabolizer.Comp.CachedWasteSolutionEnt = wasteSol.Value.Owner;
            dirty = true;
        }
        if (dirty)
            Dirty(metabolizer);
    }
}
