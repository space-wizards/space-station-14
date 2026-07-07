using Robust.Shared.GameStates;

namespace Content.Shared.Stacks;

/// <summary>
/// Denotes an item as having thresholded stack visuals.
/// StackComponent.LayerFunction should be set to Threshold to use this in practice.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StackLayerThresholdComponent : Component
{
    /// <summary>
    /// A list of thresholds to check against the number of things in the stack.
    /// Each exceeded threshold will cause the next layer to be displayed.
    /// Should be sorted in ascending order.
    /// </summary>
    [DataField(required: true)]
    public List<int> Thresholds = new();
}
