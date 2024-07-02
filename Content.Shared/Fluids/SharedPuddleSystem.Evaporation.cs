using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Shared.Fluids;

public abstract partial class SharedPuddleSystem
{
    public static readonly ProtoId<ReagentPrototype>[] EvaporationReagents = ["Water", "WeldingFuel"];

    public bool CanFullyEvaporate(Solution solution)
    {
        return solution.GetTotalPrototypeQuantity(EvaporationReagents) == solution.Volume;
    }
}
