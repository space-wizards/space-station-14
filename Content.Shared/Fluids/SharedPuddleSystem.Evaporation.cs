using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;

namespace Content.Shared.Fluids;

public abstract partial class SharedPuddleSystem
{
    public string[] GetEvaporatingReagents(Solution solution)
    {
        var evaporatingReagents = new List<string>();
        foreach (ReagentPrototype solProto in solution.GetReagentPrototypes(_prototypeManager).Keys)
        {
            if (solProto.EvaporationSpeed > FixedPoint2.Zero)
                evaporatingReagents.Add(solProto.ID);
        }
        return evaporatingReagents.ToArray();
    }

    public string[] GetAbsorbentReagents(Solution solution)
    {
        var absorbentReagents = new List<string>();
        foreach (ReagentPrototype solProto in solution.GetReagentPrototypes(_prototypeManager).Keys)
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
    /// Gets the evaporating speed of the reagents within a solution.
    /// The speed at which a solution evaporates is the sum of the speed of all evaporating reagents in it.
    /// </summary>
    public Dictionary<string, FixedPoint2> GetEvaporationSpeeds(Solution solution)
    {
        var evaporatingSpeeds = new Dictionary<string, FixedPoint2>();
        foreach (ReagentPrototype solProto in solution.GetReagentPrototypes(_prototypeManager).Keys)
        {
            if (solProto.EvaporationSpeed > FixedPoint2.Zero)
            {
                evaporatingSpeeds.Add(solProto.ID, solProto.EvaporationSpeed);
            }
        }
        return evaporatingSpeeds;
    }
}
