using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;

namespace Content.Shared.Fluids;

public abstract partial class SharedPuddleSystem
{
    [ValidatePrototypeId<ReagentPrototype>]
    private const string Water = "Water";

    public static readonly string[] EvaporationReagents = [Water];

    public string[] EvaporatableProtosInSolution(Solution solution)
    {
        List<string> evaporationReagents = [];

        foreach (var (reagent, _) in solution.Contents)
        {
            var reagentProto = _prototypeManager.Index<ReagentPrototype>(reagent.Prototype);
            if (reagentProto.Evaporates)
                evaporationReagents.Add(reagentProto.ID);
        }
        return evaporationReagents.ToArray();
    }
    
    public bool SolutionHasEvaporation(Solution solution)
    {
        foreach (var (reagent, quantity)  in solution.Contents)
        {
            var reagentProto = _prototypeManager.Index<ReagentPrototype>(reagent.Prototype);
            if (reagentProto.Evaporates && quantity > FixedPoint2.Zero)
                return true;
        }
        return false;
    }

    public bool CanFullyEvaporate(Solution solution)
    {
        return solution.GetTotalPrototypeQuantity(EvaporatableProtosInSolution(solution)) == solution.Volume;
    }
}
