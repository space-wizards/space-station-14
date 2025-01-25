using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids;
using Content.Shared.Fluids.Components;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._Impstation.ImpEvaporation;

public abstract partial class SharedImpEvaporationSystem : EntitySystem
{
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPuddleSystem _puddleSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ImpEvaporationComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(EntityUid uid, ImpEvaporationComponent comp, ComponentInit args)
    {
        comp.NextTick = _timing.CurTime + comp.EvaporationCooldown;
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

    public void TickEvaporation()
    {
        var enumerator = EntityQueryEnumerator<ImpEvaporationComponent, PuddleComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();
        var curTime = _timing.CurTime;
        while (enumerator.MoveNext(out var uid, out var evap, out var puddle))
        {
            List<(float, string)> removalQuantities = [];

            if (evap.NextTick > curTime)
                continue;

            evap.NextTick += evap.EvaporationCooldown;

            if (!_solution.ResolveSolution(uid, puddle.SolutionName, ref puddle.Solution, out var puddleSolution))
                continue;

            foreach (var reagent in puddleSolution)
            {
                var reagentProto = _prototypeManager.Index<ReagentPrototype>(reagent.Reagent.Prototype);

                var reagentName = reagentProto.ID.ToString();

                if (reagentName != null && reagentProto.ImpEvaporates)
                    removalQuantities.Add((reagentProto.ImpEvaporationAmount, reagentName));
            }

            foreach (var (amount, reagent) in removalQuantities)
            {
                puddleSolution.SplitSolutionWithOnly(amount, reagent);
            }

            if (puddleSolution.Volume == FixedPoint2.Zero)
            {
                Spawn("PuddleSparkle", xformQuery.GetComponent(uid).Coordinates);
                QueueDel(uid);
            }
        }
    }
}
