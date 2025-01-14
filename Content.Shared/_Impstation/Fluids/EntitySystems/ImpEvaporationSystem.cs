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

        // grab the current time before running the loop
        var curTime = _timing.CurTime;

        // for passing the puddle's uid outside of the loop
        EntityUid? gotPuddle = null;

        // for passing the puddle's solution outside of the loop
        Solution? gotSolution = null;

        // list of tuples. contains the name of each reagent with evaporation, with its evaporation amount.
        List<(string, float)> reagentsToRemove = [];

        // GET THE EVAPORATION INFORMATION FROM EACH PUDDLE
        // while the EQE is looking at this entity,
        while (enumerator.MoveNext(out var uid, out var comp))
        {
            // skip logic if the cooldown isn't up
            if (comp.NextTick > curTime)
            {
                continue;
            }

            // check for a solution. if it's there, grab it as our `solution` variable. then check if it has fluid in it.
            if (!_solution.TryGetSolution(uid, comp.Solution, out _, out var solution))
            {
                continue;
            }

            // pass the solution outside of the while loop
            gotSolution = solution;

            // pass the uid outside of the while loop
            gotPuddle = uid;

            // check if the solution has any reagents that have evaporation.
            if (!SolutionHasEvaporation(solution))
            {
                continue;
            }

            // now set the next tick to the current tick plus the cooldown time.
            // we do this now because it doesn't really matter for puddles that are going to be deleted or RemComp'd.
            comp.NextTick += comp.EvaporationCooldown;

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
                    // we do this outside of the while loop by passing the name and evaporation amount as a tuple for use later.
                    reagentsToRemove.Add((reagentId, reagentProto.ImpEvaporationAmount));
                }
            }
        }
        // after we have all that information,

        // DO THE EVAPORATION LOGIC
        // if there's a puddle, which there should be if we got to this point,
        if (gotPuddle != null)
        {
            // DELETE EMPTY PUDDLES
            // if the last puddle we went over had no solution, or if the solution is empty,
            if (gotSolution != null && gotSolution.Volume == FixedPoint2.Zero)
            {
                // grab the coordinates of that puddle,
                var puddleCoords = gotPuddle!.Value.ToCoordinates();

                // spawn a sparkle there,
                Spawn("PuddleSparkle", puddleCoords);

                // delete the entity,
                QueueDel(gotPuddle);
            }

            // REMCOMP PUDDLES THAT DON'T HAVE EVAPORATION REAGENTS IN THEM
            if (gotSolution != null && !SolutionHasEvaporation(gotSolution))
            {
                Log.Debug($"RemComping {gotPuddle}");
                RemComp<ImpEvaporationComponent>(gotPuddle!.Value);
            }

            // REMOVE THE REAGENTS THAT NEED REMOVING
            if (gotSolution != null)
            {
                Log.Debug($"Removing reagents from {gotPuddle}. reagentsToRemove = {reagentsToRemove}");
                foreach (var (reagent, amount) in reagentsToRemove)
                {
                    var reagentQuantity = new ReagentQuantity(reagent, amount);
                    gotSolution.RemoveReagent(reagentQuantity);
                }
            }
        }
    }
}
