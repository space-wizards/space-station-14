using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using System;

namespace Content.Shared.Foldable;

/// <summary>
/// Used to create "foldable structures" that you can pickup like an item when folded. Used for rollerbeds and wheelchairs.
/// </summary>
/// <remarks>
/// Wiill prevent any insertions into containers while this item is unfolded.
/// </remarks>
[RegisterComponent]
[NetworkedComponent]
[Friend(typeof(SharedFoldableSystem))]
public sealed class FoldableComponent : Component
{
    [DataField("folded")]
    public bool IsFolded = false;
}

// ahhh, the ol' "state thats just a copy of the component".
[Serializable, NetSerializable]
public sealed class FoldableComponentState : ComponentState
{
    public readonly bool IsFolded;

    public FoldableComponentState(bool isFolded)
    {
        IsFolded = isFolded;
    }
}
