using Content.Shared.Body.Events;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Medical.Metabolism.Components;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Medical.Metabolism.Systems;


/// <summary>
/// Handles celluar respiration aka converting Oxygen -> Co2
/// </summary>
public sealed class MetabolismSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly DamageableSystem _damageSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly INetManager _netManager = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MetabolizerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MetabolizerComponent, BodyInitializedEvent>(OnBodyInit);
    }

    private void OnBodyInit(EntityUid uid, MetabolizerComponent metabolizer, BodyInitializedEvent args)
    {
        if (!metabolizer.UsesBodySolution)
            return;
        UpdateCachedSolutions((uid, metabolizer), args.Body);
    }

    private void OnMapInit(EntityUid uid, MetabolizerComponent metabolizer, MapInitEvent args)
    {
        if (_netManager.IsClient)
            return;

        //prevent accidents, yaml should not be setting values in these but just in case...
        metabolizer.CachedAbsorbedReagents.Clear();
        metabolizer.CachedWasteReagents.Clear();
        var metabolismType = _protoManager.Index(metabolizer.MetabolismType);
        foreach (var (reagentId, minAmt) in metabolismType.RequiredReagents)
        {
            metabolizer.CachedAbsorbedReagents.Add(new ReagentQuantity(reagentId, minAmt, null));
        }
        foreach (var (reagentId, minAmt) in metabolismType.WasteReagents)
        {
            metabolizer.CachedWasteReagents.Add(new ReagentQuantity(reagentId, minAmt, null));
        }

        if (metabolizer.UsesBodySolution) //if we are using a solution on body, differ initializing cached solutions
            return;
        metabolizer.CachedDeprivationDamage = metabolismType.DeprivationDamage;
        UpdateCachedSolutions((uid, metabolizer), uid);
    }
    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<MetabolizerComponent>();
        while (query.MoveNext(out var uid, out var metabolism))
        {
            if (_gameTiming.CurTime < metabolism.NextUpdate)
                continue;

            metabolism.NextUpdate += metabolism.UpdateInterval;
            if (!TryAbsorbReagents((uid,metabolism)))
            {
                _damageSystem.TryChangeDamage(uid,
                    metabolism.CachedDeprivationDamage,
                    true,
                    false);
                //TODO: block entity healing
            }
        }
    }

    private void UpdateCachedSolutions(Entity<MetabolizerComponent> respirator,
        EntityUid? target)
    {
        target ??= respirator.Owner;

        var dirty = false;
        if (_solutionContainerSystem.TryGetSolution((target.Value, null), respirator.Comp.AbsorbSolutionId, out var absorbSol, true))
        {
            respirator.Comp.CachedAbsorbSolutionEnt = absorbSol.Value.Owner;
            dirty = true;
        }
        if (_solutionContainerSystem.TryGetSolution((target.Value, null), respirator.Comp.WasteSolutionId, out var wasteSol, true))
        {
            respirator.Comp.CachedWasteSolutionEnt = wasteSol.Value.Owner;
            dirty = true;
        }
        if (dirty)
            Dirty(respirator);
    }

    private bool TryAbsorbReagents(Entity<MetabolizerComponent> metabolism)
    {
        if (metabolism.Comp.CachedAbsorbSolutionEnt == EntityUid.Invalid
            || metabolism.Comp.CachedWasteSolutionEnt == EntityUid.Invalid)
            return false;
        var absorbSol = Comp<SolutionComponent>(metabolism.Comp.CachedAbsorbSolutionEnt);
        var wasteSol = Comp<SolutionComponent>(metabolism.Comp.CachedWasteSolutionEnt);
        //TODO: some of this can be simplified if/when there is a common unit reactions calculation method in chem
        foreach (var (reagentId, quantity) in metabolism.Comp.CachedAbsorbedReagents)
        {
            var reqQuant = quantity*metabolism.Comp.BaseMultiplier;
            if (!absorbSol.Solution.TryGetReagentQuantity(reagentId, out var solQuant) || solQuant < reqQuant)
            {
                return false;
            }
        }


        //possibly add product rate limiting, to prevent reactions when the waste concentration is too high?
        //that would mean that high bloodCO2 levels would prevent respiration, which is interesting but
        //not sure if the benefits to doing that are worth the costs for added (limited) gameplay depth and "realism"
        foreach (var (reagentId, quantity) in metabolism.Comp.CachedAbsorbedReagents)
        {
            var reqQuant = quantity*metabolism.Comp.BaseMultiplier;
            absorbSol.Solution.RemoveReagent(reagentId, reqQuant);
        }
        Dirty(metabolism.Comp.CachedAbsorbSolutionEnt, absorbSol);

        if (metabolism.Comp.CachedWasteReagents.Count == 0)
            return true; //if there are no waste products then exit out
        foreach (var (reagentId, quantity) in metabolism.Comp.CachedWasteReagents)
        {
            var reqQuant = quantity*metabolism.Comp.BaseMultiplier;
            absorbSol.Solution.AddReagent(reagentId, reqQuant);
        }
        _solutionContainerSystem.UpdateChemicals((metabolism.Comp.CachedWasteSolutionEnt,wasteSol));
        return true;
    }


}
