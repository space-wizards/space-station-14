using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;

namespace Content.Shared._Offbrand.Wounds;

public static class SolutionExtensions
{
    public static bool HasOverlapAtLeast(this Solution solution, Solution incoming, FixedPoint2 threshold)
    {
        var count = FixedPoint2.Zero;

        foreach (var (reagent, quantity) in incoming.Contents)
        {
            count += solution.GetReagentQuantity(reagent);
        }

        return count >= threshold;
    }
}
