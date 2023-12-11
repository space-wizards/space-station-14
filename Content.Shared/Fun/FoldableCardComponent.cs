using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.FoldableCard;

/// <summary>
/// Used to create "foldable structures" that you can pickup like an item when folded. Used for rollerbeds and wheelchairs.
/// </summary>
/// <remarks>
/// Wiill prevent any insertions into containers while this item is unfolded.
/// </remarks>
[RegisterComponent]
[NetworkedComponent]
[Access(typeof(FoldableCardSystem))]
public sealed partial class FoldableCardComponent : Component
{
    [DataField("folded")]
    public bool IsFolded = false;

    [DataField("Description")]
    public string Description = string.Empty;

    public string CurrentDescription = string.Empty;
}

// ahhh, the ol' "state thats just a copy of the component".
[Serializable, NetSerializable]
public sealed class FoldableCardComponentState : ComponentState
{
    public readonly bool IsFolded;

    public FoldableCardComponentState(bool isFolded)
    {
        IsFolded = isFolded;
    }
}
