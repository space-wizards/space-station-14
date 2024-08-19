using Content.Shared.Physics;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Dynamics;

namespace Content.Shared.Foldable;

/// <summary>
/// Used to create "foldable structures" that you can pickup like an item when folded.
/// </summary>
/// <remarks>
/// Will prevent any insertions into containers while this item is unfolded.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(FoldableSystem))]
public sealed partial class FoldableComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool IsFolded = false;

    // Responsible for disabling collision when item is unfolded
    [DataField, AutoNetworkedField]
    public bool FoldedDisableCollision = false;

    [DataField]
    public bool CanFoldInsideContainer = false;

    // Can fold or unfold it by hands
    [DataField, AutoNetworkedField]
    public bool CanBeHandlyFolded = true;

    [DataField]
    public LocId UnfoldVerbText = "unfold-verb";

    [DataField]
    public LocId FoldVerbText = "fold-verb";
}
