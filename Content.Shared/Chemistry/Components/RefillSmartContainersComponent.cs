using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameStates;

namespace Content.Shared.Chemistry.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class RefillSmartContainersComponent : Component
{
    private static ListEqualityComparer _comparer = new ListEqualityComparer();
    /// <summary>
    /// A dictionary of reagent strings and the jug entID that contains the reagent.
    /// Any jug added to the container is added to the list. Same with removal.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<List<ReagentId>, List<Entity<SolutionComponent>?>> SolutionContents = new(_comparer);
}

internal class ListEqualityComparer : IEqualityComparer<List<ReagentId>>
{
    public bool Equals(List<ReagentId>? x, List<ReagentId>? y)
    {
        if (x == null || y == null)
            return false;
        for (var i = 0; i < x.Count; i++)
        {
            if (!y[i].ToString().Equals(x[i].ToString()))
                return false;
        }
        return true;
    }

    public int GetHashCode(List<ReagentId> obj)
    {
        return HashCode.Combine(obj.Capacity, obj.Count);
    }
}
