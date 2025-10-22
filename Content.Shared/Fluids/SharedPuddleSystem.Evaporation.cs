using System.Linq;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;

namespace Content.Shared.Fluids;

public abstract partial class SharedPuddleSystem
{
    private static readonly TimeSpan EvaporationCooldown = TimeSpan.FromSeconds(1);

    private void OnEvaporationMapInit(Entity<EvaporationComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextTick = _timing.CurTime + EvaporationCooldown;
        Dirty(ent);
    }

    private void UpdateEvaporation(EntityUid uid, Solution solution)
    {
        if (_evaporationQuery.HasComp(uid))
            return;

        if (solution.GetTotalPrototypeQuantity(GetEvaporatingReagents(solution)) > FixedPoint2.Zero)
        {
            var evaporation = AddComp<EvaporationComponent>(uid);
            evaporation.NextTick = _timing.CurTime + EvaporationCooldown;
            Dirty<EvaporationComponent>((uid, evaporation));
            return;
        }

        RemComp<EvaporationComponent>(uid);
    }

    private void TickEvaporation()
    {
        var query = EntityQueryEnumerator<EvaporationComponent, PuddleComponent>();
        var curTime = _timing.CurTime;
        while (query.MoveNext(out var uid, out var evaporation, out var puddle))
        {
            if (evaporation.NextTick > curTime)
                continue;

            // Necessary to keep client and server in sync so they don't drift
            evaporation.NextTick += EvaporationCooldown;
            Dirty(uid, evaporation);

            if (!_solutionContainerSystem.ResolveSolution(uid, puddle.SolutionName, ref puddle.Solution, out var puddleSolution))
                continue;

            // If we have multiple evaporating reagents in one puddle, just take the average evaporation speed and apply
            // that to all of them.
            var evaporationSpeeds = GetEvaporationSpeeds(puddleSolution);
            if (evaporationSpeeds.Count == 0)
                continue;

            // Can't use .Average because FixedPoint2
            var evaporationSpeed = evaporationSpeeds.Values.Sum() / evaporationSpeeds.Count;
            var reagentProportions = evaporationSpeeds.ToDictionary(kv => kv.Key,
                kv => puddleSolution.GetTotalPrototypeQuantity(kv.Key) / puddleSolution.Volume);

            // Still have to iterate over one-by-one since the full solution could have non-evaporating solutions.
            foreach (var (reagent, factor) in reagentProportions)
            {
                var reagentTick = evaporation.EvaporationAmount * EvaporationCooldown.TotalSeconds * evaporationSpeed * factor;
                puddleSolution.SplitSolutionWithOnly(reagentTick, reagent);
            }

            // Despawn if we're done
            if (puddleSolution.Volume == FixedPoint2.Zero)
            {
                // Spawn a *sparkle*
                SpawnAttachedTo(evaporation.EvaporationEffect, Transform(uid).Coordinates);
                PredictedQueueDel(uid);
            }

            _solutionContainerSystem.UpdateChemicals(puddle.Solution.Value);
        }
    }


    public string[] GetEvaporatingReagents(Solution solution)
    {
        List<string> evaporatingReagents = [];
        foreach (var solProto in solution.GetReagentPrototypes(_prototypeManager).Keys)
        {
            if (solProto.EvaporationSpeed > FixedPoint2.Zero)
                evaporatingReagents.Add(solProto.ID);
        }
        return evaporatingReagents.ToArray();
    }

    public string[] GetAbsorbentReagents(Solution solution)
    {
        List<string> absorbentReagents = [];
        foreach (var solProto in solution.GetReagentPrototypes(_prototypeManager).Keys)
        {
            if (solProto.Absorbent)
                absorbentReagents.Add(solProto.ID);
        }
        return absorbentReagents.ToArray();
    }

    public bool CanFullyEvaporate(Solution solution)
    {
        return solution.GetTotalPrototypeQuantity(GetEvaporatingReagents(solution)) == solution.Volume;
    }

    /// <summary>
    /// Gets a mapping of evaporating speed of the reagents within a solution.
    /// The speed at which a solution evaporates is the average of the speed of all evaporating reagents in it.
    /// </summary>
    public Dictionary<string, FixedPoint2> GetEvaporationSpeeds(Solution solution)
    {
        Dictionary<string, FixedPoint2> evaporatingSpeeds = [];
        foreach (var solProto in solution.GetReagentPrototypes(_prototypeManager).Keys)
        {
            if (solProto.EvaporationSpeed > FixedPoint2.Zero)
            {
                evaporatingSpeeds.Add(solProto.ID, solProto.EvaporationSpeed);
            }
        }
        return evaporatingSpeeds;
    }
}
