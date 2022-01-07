using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using System;

namespace Content.Shared.Foldable;

/// <summary>
/// Used to create "foldable structures" that you can pickup like an item when folded. Used for rollerbeds and wheelchairs
/// </summary>
[RegisterComponent]
[NetworkedComponent]
[Friend(typeof(SharedFoldableSystem))]
public class FoldableComponent : Component
{
    public override string Name => "Foldable";

    [DataField("folded")]
    [ViewVariables]
    public bool IsFolded = false;

    public bool CanBeFolded = true;
}

// ahhh, the ol' "state thats just a copy of the component".
[Serializable, NetSerializable]
public class FoldableComponentState : ComponentState
{
    public readonly bool IsFolded;
    public readonly bool CanBeFolded;

    public FoldableComponentState(bool isFolded, bool canBeFolded)
    {
        IsFolded = isFolded;
        CanBeFolded = canBeFolded;
    }
}
