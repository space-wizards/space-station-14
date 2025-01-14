using Content.Shared.Fluids;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Coordinates;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Timing;

namespace Content.Shared._Impstation.ImpEvaporation;

public sealed partial class ImpEvaporationSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPuddleSystem _puddleSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    /// <summary>
    /// Returns a list of reagents that evaporate. This was ripped directly off of space-wizards #34304, but it's basically just what I was doing but cleaner. 
    /// </summary>
    /// <param name="solution"></param>
    /// <returns></returns>
    public string[] EvaporatableProtosInSolution(Solution solution)
    {
        // declare a list which will contain reagents in the solution that have evaporation
        List<string> evaporationReagents = [];

        // for each reagent in the solution,
        foreach (var (reagent, _) in solution.Contents)
        {
            // set a variable to that reagent's prototype,
            var reagentProto = _prototypeManager.Index<ReagentPrototype>(reagent.Prototype);

            // check if the reagent should evaporate, and if it should, add it to the list.
            if (reagentProto.ImpEvaporates)
                evaporationReagents.Add(reagentProto.ID);
        }

        // then, return the list as an array.
        return evaporationReagents.ToArray();
    }

    /// <summary>
    /// Returns whether or not the solution has any puddles which evaporate. This was also ripped directly off of space-wizards #34304
    /// </summary>
    /// <param name="solution"></param>
    /// <returns></returns>
    public bool SolutionHasEvaporation(Solution solution)
    {
        // for each reagent in the solution,
        foreach (var (reagent, quantity) in solution.Contents)
        {
            // get the reagent prototype and set a value to it
            var reagentProto = _prototypeManager.Index<ReagentPrototype>(reagent.Prototype);

            // check if the prototype 1) evaporates and 2) exists
            if (reagentProto.ImpEvaporates && quantity > FixedPoint2.Zero)
                // if it does, return true.
                return true;
        }
        // else, return false.
        return false;
    }

    /// <summary>
    /// Runs every frame. Looks over every ent with this component and does evaporation logic to them.
    /// </summary>
    /// <param name="frameTime"></param>
    public override void Update(float frameTime)
    {
        // declare your EQE, which will loop through all ents with this component
        var enumerator = EntityQueryEnumerator<ImpEvaporationComponent>();

        // list of tuples. contains the uid and solution of each puddle that needs to be operated on, and the reagentquantities that need to be removed from them. 
        List<(EntityUid, Solution?, List<ReagentQuantity>, ImpEvaporationComponent)> puddlesToOperate = [];

        // GET THE EVAPORATION INFORMATION FROM EACH PUDDLE
        // for every entity with this component,
        while (enumerator.MoveNext(out var uid, out var comp))
        {
            // skip logic if the cooldown isn't up
            if (comp.NextTick > _timing.CurTime)
            {
                continue;
            }

            // check for a solution. if it's there, grab it as our `solution` variable. if not, skip it for now.
            if (!_solution.TryGetSolution(uid, comp.Solution, out _, out var solution))
            {
                continue;
            }

            // check if the solution has any reagents that have evaporation. if not, skip it for now.
            if (!SolutionHasEvaporation(solution))
            {
                continue;
            }

            // create a new list of ReagentQuantities to be gathered from the next foreach
            List<ReagentQuantity> reagentQuantities = [];

            // now, for each reagent in the solution,
            foreach (var (reagent, _) in solution.Contents)
            {
                // grab its ReagentPrototype,
                var reagentProto = _prototypeManager.Index<ReagentPrototype>(reagent.Prototype);

                // grab its name,
                var reagentId = reagentProto.ID.ToString();

                // check if it 1) exists and 2) evaporates
                if (reagentId != null && reagentProto.ImpEvaporates)
                {
                    // and remove an amount of it equal to its EvaporationAmount.

                    // we do this by declaring a new reagentQuantity and add it to the list of ReagentQuantities we made earlier,
                    var reagentQuantity = new ReagentQuantity(reagentId, reagentProto.ImpEvaporationAmount);
                    reagentQuantities.Add(reagentQuantity);
                }
            }

            // now set the next tick to the current tick plus the cooldown time.
            // we do this now because it doesn't really matter for puddles that are going to be deleted or RemComp'd.
            comp.NextTick += comp.EvaporationCooldown;

            // and then passing the uid, solution, and reagentQuantities as a tuple for use outside the loop.
            puddlesToOperate.Add((uid, solution, reagentQuantities, comp));
        }
        // after we have all that information,

        // DO THE EVAPORATION LOGIC ON EACH PUDDLE
        // if there's a puddle, which there should be if we got to this point,

        foreach (var (puddle, solution, reagents, comp) in puddlesToOperate)
        {
            if (solution == null || solution.Volume == FixedPoint2.Zero)
            {
                var puddleCoords = puddle.ToCoordinates();

                Spawn("PuddleSparkle", puddleCoords);

                QueueDel(puddle);
            }

            else
            {
                if (!SolutionHasEvaporation(solution))
                {
                    RemComp<ImpEvaporationComponent>(puddle);
                }

                foreach (var reagentQuantity in reagents)
                {
                    solution!.RemoveReagent(reagentQuantity);
                }
            }
        }
    }
}
