using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Foldable;

/// <summary>
/// Used to create "foldable structures" that you can pickup like an item when folded. Used for rollerbeds and wheelchairs.
/// </summary>
/// <remarks>
/// Wiill prevent any insertions into containers while this item is unfolded.
/// </remarks>
[RegisterComponent]
[NetworkedComponent]
[Access(typeof(FoldableSystem))]
public sealed partial class FoldableComponent : Component
{
    [DataField("folded")]
    public bool IsFolded = false;

    [DataField]
    public bool FitsInContainerUnfolded = false;

    [DataField]
    public bool CanFoldInsideContainer = false;

    [DataField]
    public bool CanFoldWhileEquipped = false;
}

[Serializable, NetSerializable]
public sealed class FoldableComponentState : ComponentState
{
    public readonly bool IsFolded;

    public FoldableComponentState(bool isFolded)
    {
        IsFolded = isFolded;
    }
}
