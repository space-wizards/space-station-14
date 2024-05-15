using Content.Shared.Body.Events;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Blood.Systems;
using Content.Shared.Medical.Metabolism.Components;

namespace Content.Shared.Medical.Metabolism.Systems;

public sealed partial class MetabolismSystem
{
    private void BodyMetabolismInit()
    {
        SubscribeLocalEvent<MetabolismComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MetabolismComponent, BodyInitializedEvent>(OnBodyInit);
        SubscribeLocalEvent<MetabolismComponent, ComponentInit>(OnCompInit);
    }

    private void OnCompInit(EntityUid uid, MetabolismComponent metabolism, ComponentInit args)
    {
        var metabolismType = _protoManager.Index(metabolism.MetabolismType);
        if (metabolismType.EnergyReagent != null)
            metabolism.CachedEnergyReagent = new ReagentId(metabolismType.EnergyReagent.Value, null);
        metabolism.CachedKCalPerReagent = metabolismType.KCalPerEnergyReagent;
        metabolism.CachedDeprivationDamage = metabolismType.DeprivationDamage;
        metabolism.CachedReagentTarget = metabolismType.TargetEnergyReagentConcentration;
        if (metabolism.CalorieBuffer < 0)
            metabolism.CalorieBuffer = metabolism.CalorieBufferCap;
    }

    private void OnBodyInit(EntityUid uid, MetabolismComponent metabolism, BodyInitializedEvent args)
    {
        if (!metabolism.UsesBodySolution)
            return;
        UpdateCachedBodyMetabolismSolutions((uid, metabolism), args.Body);
        InitializeBodyMetabolism((uid, metabolism));
    }

    private void OnMapInit(EntityUid uid, MetabolismComponent metabolism, MapInitEvent args)
    {
        if (_netManager.IsClient)
            return;

        if (metabolism.UsesBodySolution)
            return;
        UpdateCachedBodyMetabolismSolutions((uid, metabolism), null);
        InitializeBodyMetabolism((uid, metabolism));
    }

    private void InitializeBodyMetabolism(Entity<MetabolismComponent> metabolism)
    {
        Entity<SolutionComponent> solEnt = (metabolism.Comp.CachedSolutionEnt,Comp<SolutionComponent>(metabolism.Comp.CachedSolutionEnt));
        if (metabolism.Comp.CachedEnergyReagent == null)
            return;
        solEnt.Comp.Solution.AddReagent(metabolism.Comp.CachedEnergyReagent.Value,
            metabolism.Comp.CachedReagentTarget * BloodstreamSystem.BloodstreamVolumeTEMP);
    }


    private void UpdateBodyMetabolism(float frameTime)
    {
        var query = EntityQueryEnumerator<MetabolismComponent>();
        while (query.MoveNext(out var uid, out var metabolism))
        {
            if (_gameTiming.CurTime < metabolism.NextUpdate)
                continue;

            metabolism.NextUpdate += metabolism.UpdateInterval;
            UpdateEnergy((uid, metabolism));
        }
    }

    private void UpdateEnergy(Entity<MetabolismComponent> metabolism)
    {
        if (!TryComp<SolutionComponent>(metabolism.Comp.CachedSolutionEnt, out var comp))
            return;
        Entity<SolutionComponent> solEnt = (metabolism.Comp.CachedSolutionEnt, comp);
        //Entity<SolutionComponent> solEnt = (metabolism.Comp.CachedSolutionEnt,Comp<SolutionComponent>(metabolism.Comp.CachedSolutionEnt));
        if (metabolism.Comp.CachedEnergyReagent == null
            || !TryGetBloodEnergyConc(metabolism, solEnt, BloodstreamSystem.BloodstreamVolumeTEMP,out var concentration))
            return;
        var reagentDelta = (concentration - metabolism.Comp.CachedReagentTarget)* BloodstreamSystem.BloodstreamVolumeTEMP;
        if (reagentDelta == 0)
            return;
        //invert the delta because we're trying to make up for it
        if (ChangeStoredEnergy(metabolism, solEnt, reagentDelta))
            return;
        Log.Debug($"{ToPrettyString(metabolism)} is starving!");
        //TODO: add starving effects herews
    }

    private bool ChangeStoredEnergy(Entity<MetabolismComponent> metabolism,
        Entity<SolutionComponent> solEnt,
        FixedPoint2 reagentDelta)
    {
        if (metabolism.Comp.CachedEnergyReagent == null)
            return false;
        if (reagentDelta == 0)
            return true;
        var calorieDelta = reagentDelta.Float() * metabolism.Comp.CachedKCalPerReagent;
        if (calorieDelta > 0)
        {
            if (metabolism.Comp.CalorieBuffer == 0)
            {
                Log.Debug($"{ToPrettyString(metabolism)} has started filling it's calorie buffer");
            }
            if (metabolism.Comp.CalorieBuffer < metabolism.Comp.CalorieBufferCap)
            {
                metabolism.Comp.CalorieBuffer += calorieDelta;
                var overflow = metabolism.Comp.CalorieBufferCap - metabolism.Comp.CalorieBuffer;
                if (overflow < 0)
                {
                    metabolism.Comp.CalorieBuffer = metabolism.Comp.CalorieBufferCap;
                    Log.Debug($"{ToPrettyString(metabolism)} has overflowed it's calorie buffer and is now adding to storage.");
                    metabolism.Comp.CalorieStorage -= overflow;
                    Dirty(metabolism);
                    solEnt.Comp.Solution.RemoveReagent(metabolism.Comp.CachedEnergyReagent.Value, reagentDelta);
                    _solutionContainerSystem.UpdateChemicals(solEnt, true, false);
                    return true;
                }
                Dirty(metabolism);
                solEnt.Comp.Solution.RemoveReagent(metabolism.Comp.CachedEnergyReagent.Value, reagentDelta);
                _solutionContainerSystem.UpdateChemicals(solEnt, true, false);
                return true;
            }
            metabolism.Comp.CalorieStorage += calorieDelta;
            Dirty(metabolism);
            solEnt.Comp.Solution.RemoveReagent(metabolism.Comp.CachedEnergyReagent.Value, reagentDelta);
            _solutionContainerSystem.UpdateChemicals(solEnt, true, false);
            return true;
        }

        var underflow = 0f;
        //Negative calorie delta so operations should be inverted!!

        if (metabolism.Comp.CalorieStorage == 0 || metabolism.Comp.CalorieBuffer == 0)
            return false;
        if (metabolism.Comp.CalorieBuffer < 0)
            metabolism.Comp.CalorieBuffer = 0;

        underflow = metabolism.Comp.CalorieBuffer += calorieDelta;
        if (underflow >= 0)
        {
            Dirty(metabolism);
            solEnt.Comp.Solution.AddReagent(metabolism.Comp.CachedEnergyReagent.Value, -reagentDelta);
            _solutionContainerSystem.UpdateChemicals(solEnt, true, false);
            return true;
        }

        if (metabolism.Comp.CalorieBuffer == 0)
        {
            Log.Debug($"{ToPrettyString(metabolism)} calorie buffer is empty!");
        }


        metabolism.Comp.CalorieStorage += underflow;
        if (metabolism.Comp.CalorieStorage < 0)
        {
            metabolism.Comp.CalorieStorage = 0;
            Dirty(metabolism);
            solEnt.Comp.Solution.AddReagent(metabolism.Comp.CachedEnergyReagent.Value, -reagentDelta);
            _solutionContainerSystem.UpdateChemicals(solEnt, true, false);
            return false;
        }
        Dirty(metabolism);
        solEnt.Comp.Solution.AddReagent(metabolism.Comp.CachedEnergyReagent.Value, -reagentDelta);
        _solutionContainerSystem.UpdateChemicals(solEnt, true, false);
        return true;
    }

    private bool TryGetBloodEnergyConc(Entity<MetabolismComponent> metabolism,
        Entity<SolutionComponent> solution,
        FixedPoint2 volume,
        out float concentration)
    {
        concentration = 0;
        if (metabolism.Comp.CachedEnergyReagent == null)
            return false;
        concentration = _solutionContainerSystem.GetReagentConcentration(solution,
            volume,
            metabolism.Comp.CachedEnergyReagent.Value);
        return true;
    }


    private void UpdateCachedBodyMetabolismSolutions(Entity<MetabolismComponent> metabolism,
        EntityUid? target)
    {
        target ??= metabolism.Owner;

        var dirty = false;
        if (_solutionContainerSystem.TryGetSolution((target.Value, null), metabolism.Comp.AbsorbSolutionId, out var absorbSol, true))
        {
            metabolism.Comp.CachedSolutionEnt = absorbSol.Value.Owner;
            dirty = true;
        }
        if (dirty)
            Dirty(metabolism);
    }
}
