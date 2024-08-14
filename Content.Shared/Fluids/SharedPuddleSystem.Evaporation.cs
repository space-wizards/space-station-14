using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.Solutions;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;

namespace Content.Shared.Fluids;

public abstract partial class SharedPuddleSystem
{
    //TODO: Dear god why! Why would you hardcode this?!
    //Why is a single method in a partial?!
    [ValidatePrototypeId<ReagentPrototype>]
    private const string Water = "Water";

    public static readonly string[] EvaporationReagents = [Water];

    public bool CanFullyEvaporate(Entity<SolutionComponent> solution)
    {
        return _solutionSystem.GetTotalQuantity(solution, EvaporationReagents) == solution.Comp.Volume;
    }
}
