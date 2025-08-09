using System.Linq;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Organ;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Body.Systems;

public sealed class StomachSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;

    public const string DefaultSolutionName = "stomach";

    public override void Initialize()
    {
        SubscribeLocalEvent<StomachComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<StomachComponent, EntityUnpausedEvent>(OnUnpaused);
        SubscribeLocalEvent<StomachComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
        SubscribeLocalEvent<StomachComponent, ApplyMetabolicMultiplierEvent>(OnApplyMetabolicMultiplier);
        SubscribeLocalEvent<StomachComponent, SolutionContainerChangedEvent>(OnSolutionContainerChanged);
    }

    private void OnMapInit(Entity<StomachComponent> ent, ref MapInitEvent args)
    {
        var curTime = _gameTiming.CurTime;
        ent.Comp.NextUpdate = curTime + ent.Comp.UpdateInterval;
        // TODO: this is inaccurate for persistence; this should be changed once
        // https://github.com/space-wizards/RobustToolbox/issues/3768 is fixed
        // That being said, we don't serialize stuff like BaseMob so I think
        // this only matters for... not much, but it's the thought that counts.
        UpdateDigestionTimes(ent, _ => curTime + ent.Comp.AdjustedDigestionDelay);
    }

    private void OnUnpaused(Entity<StomachComponent> ent, ref EntityUnpausedEvent args)
    {
        var time = args.PausedTime;
        ent.Comp.NextUpdate += time;
        UpdateDigestionTimes(ent, t => t + time);
    }

    private void OnEntRemoved(Entity<StomachComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        // Make sure the removed entity was our contained solution
        if (ent.Comp.Solution is not { } solution || args.Entity != solution.Owner)
            return;

        // Cleared our cached reference to the solution entity
        ent.Comp.Solution = null;
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<StomachComponent, OrganComponent, SolutionContainerManagerComponent>();
        var curTime = _gameTiming.CurTime;
        while (query.MoveNext(out var uid, out var stomach, out var organ, out var sol))
        {
            if (curTime < stomach.NextUpdate)
                continue;

            stomach.NextUpdate += stomach.UpdateInterval;

            // Get our solutions
            if (!_solutionContainerSystem.ResolveSolution((uid, sol),
                    DefaultSolutionName,
                    ref stomach.Solution,
                    out var stomachSolution))
                continue;

            if (organ.Body is not { } body
                || !_solutionContainerSystem.TryGetSolution(body, stomach.BodySolutionName, out var bodySolution))
                continue;

            var transferSolution = new Solution();

            foreach (var (id, deltas) in stomach.ReagentDeltas.ToList())
            {
                // Get the deltas that we can now digest
                var expiredDeltas = deltas
                    .Where(delta => curTime >= delta.DigestionTime)
                    .ToList();

                foreach (var (quantity, _) in expiredDeltas)
                {
                    // Weird, but it'll get removed from our tracking later
                    if (!stomachSolution.TryGetReagent(quantity.Reagent, out var reagent))
                        continue;

                    if (reagent.Quantity > quantity.Quantity)
                        reagent = new(reagent.Reagent, quantity.Quantity);

                    stomachSolution.RemoveReagent(reagent);
                    transferSolution.AddReagent(reagent);
                }

                // Clean up the entry if it's empty
                if (stomach.ReagentDeltas[id].Count == expiredDeltas.Count)
                    stomach.ReagentDeltas.Remove(id);
                else
                    stomach.ReagentDeltas[id] = deltas.Except(expiredDeltas).ToList();
            }

            _solutionContainerSystem.UpdateChemicals(stomach.Solution.Value);

            // Transfer everything to the body solution!
            _solutionContainerSystem.TryAddSolution(bodySolution.Value, transferSolution);
        }
    }

    private void OnApplyMetabolicMultiplier(Entity<StomachComponent> ent, ref ApplyMetabolicMultiplierEvent args)
    {
        var curTime = _gameTiming.CurTime;
        var multiplier = args.Multiplier;
        UpdateDigestionTimes(ent, t => curTime + multiplier/ent.Comp.DigestionDelayMultiplier * (t - curTime));
        ent.Comp.DigestionDelayMultiplier = multiplier;
    }

    private void OnSolutionContainerChanged(Entity<StomachComponent> ent, ref SolutionContainerChangedEvent args)
    {
        if (args.SolutionId != DefaultSolutionName)
            return;

        var curTime = _gameTiming.CurTime;
        foreach (var reagent in args.Solution.Contents)
        {
            var known = ent.Comp.ReagentDeltas.GetOrNew(reagent.Reagent);
            var knownQuantity = known.Sum(delta => delta.Reagent.Quantity);

            if (reagent.Quantity <= knownQuantity)
                continue;

            // If the stomach now has a higher quantity of a reagent than what
            // we've tracked, add it as a new delta. Otherwise... we don't
            // bother, as it'll be removed in the stomach update loop. It would
            // be more correct to handle that here, but... it'd be kinda tricky
            // and I was told it's fine for now™.
            var newQuantity = new ReagentQuantity(reagent.Reagent, reagent.Quantity - knownQuantity);
            known.Add(new ReagentDelta(newQuantity, curTime + ent.Comp.AdjustedDigestionDelay));
        }
    }

    private void UpdateDigestionTimes(Entity<StomachComponent> ent, Func<TimeSpan, TimeSpan> func)
    {
        foreach (var (id, deltas) in ent.Comp.ReagentDeltas)
        {
            foreach (var (i, delta) in deltas.Index().ToList())
            {
                ent.Comp.ReagentDeltas[id][i] = delta with { DigestionTime = func(delta.DigestionTime) };
            }
        }
    }

    public bool CanTransferSolution(
        EntityUid uid,
        Solution solution,
        StomachComponent? stomach = null,
        SolutionContainerManagerComponent? solutions = null)
    {
        return Resolve(uid, ref stomach, ref solutions, logMissing: false)
               && _solutionContainerSystem.ResolveSolution((uid, solutions), DefaultSolutionName, ref stomach.Solution, out var stomachSolution)
               // TODO: For now no partial transfers. Potentially change by design
               && stomachSolution.CanAddSolution(solution);
    }

    public bool TryTransferSolution(
        EntityUid uid,
        Solution solution,
        StomachComponent? stomach = null,
        SolutionContainerManagerComponent? solutions = null)
    {
        if (!Resolve(uid, ref stomach, ref solutions, logMissing: false)
            || !_solutionContainerSystem.ResolveSolution((uid, solutions), DefaultSolutionName, ref stomach.Solution)
            || !CanTransferSolution(uid, solution, stomach, solutions))
        {
            return false;
        }

        return _solutionContainerSystem.TryAddSolution(stomach.Solution.Value, solution);
    }
}
