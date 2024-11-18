using System.Linq;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Shared.Fluids;

public abstract partial class SharedPuddleSystem
{
    public string[] GetEvaporatingReagents(Solution solution)
    {
        return solution.GetReagentPrototypes(_prototypeManager).Keys.Where(x => x.Evaporates).Select(x => x.ID).ToArray();
    }

    public string[] GetMoppableReagents(Solution solution)
    {
        return solution.GetReagentPrototypes(_prototypeManager).Keys.Where(x => x.Moppable).Select(x => x.ID).ToArray();
    }

    public bool CanFullyEvaporate(Solution solution)
    {
        return solution.GetTotalPrototypeQuantity(GetEvaporatingReagents(solution)) == solution.Volume;
    }
}
