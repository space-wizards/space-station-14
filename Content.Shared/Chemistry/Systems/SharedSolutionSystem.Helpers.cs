

using Content.Shared.Chemistry.Components.Solutions;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;

namespace Content.Shared.Chemistry.Systems;

public partial class SharedSolutionSystem
{
    public FixedPoint2 GetTotalQuantity(
        Entity<SolutionComponent> solution,
        params string[] reagents)
    {
        FixedPoint2 total = 0;
        foreach (ref var reagentId in reagents.AsSpan())
        {
            total += GetTotalQuantity(solution, new ReagentSpecifier(reagentId));
        }
        return total;
    }

    public FixedPoint2 GetTotalQuantity(
        Entity<SolutionComponent> solution,
        params ReagentSpecifier[] reagents)
    {
        FixedPoint2 total = 0;
        foreach (ref var reagent in reagents.AsSpan())
        {
            total += GetTotalQuantity(solution, reagent);
        }
        return total;
    }

    public FixedPoint2 GetTotalQuantity(
        Entity<SolutionComponent> solution,
        ReagentSpecifier reagent)
    {
        return ResolveSpecifier(ref reagent)
            ? GetTotalQuantity(solution, new ReagentDef(reagent.CachedDefinitionEntity!.Value, reagent.Variant))
            : 0;
    }
}
