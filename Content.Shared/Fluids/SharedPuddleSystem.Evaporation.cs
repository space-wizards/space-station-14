using System.Linq;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Shared.Fluids;

public abstract partial class SharedPuddleSystem
{
    public string[] GetEvaporatingReagents(Solution solution)
    {
        return solution.Contents.Where(x =>
        {
            //Check if the prototype for the ReagentId is slippery.
            _prototypeManager.TryIndex(x.Reagent.Prototype, out ReagentPrototype? proto);
            return proto?.Evaporates is bool value && value;
        }).Select(x => x.Reagent.Prototype).ToArray();
    }

    public bool CanFullyEvaporate(Solution solution)
    {
        return solution.GetTotalPrototypeQuantity(GetEvaporatingReagents(solution)) == solution.Volume;
    }
}
