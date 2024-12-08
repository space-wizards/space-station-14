using System.Runtime.InteropServices;
using Content.Shared.Chemistry.Components.Solutions;
using Content.Shared.FixedPoint;
using Robust.Shared.Map;

namespace Content.Shared.Chemistry.Systems;

public partial class SharedSolutionSystem
{
    public FixedPoint2 DoTileReactions(TileRef targetTile, Entity<SolutionComponent> solution, float percentage = 1.0f)
    {
        percentage = Math.Clamp(percentage, 0, 1f);
        FixedPoint2 removed = 0;
        var contents = CollectionsMarshal.AsSpan(solution.Comp.Contents);
        for (var i = 0; i < contents.Length; i++)
        {
            ref var reagentData = ref contents[i];
            var quantity = percentage * reagentData.TotalQuantity;
            removed += ReactiveSystem.DoTileReaction(targetTile, reagentData.ReagentEnt, quantity);
        }
        return removed;
    }


}
