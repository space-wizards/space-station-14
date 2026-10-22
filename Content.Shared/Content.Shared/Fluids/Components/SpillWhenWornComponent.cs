using Robust.Shared.GameStates;

namespace Content.Shared.Fluids.Components;

/// <summary>
/// This entity will spill its contained solution onto the wearer when worn, and its
/// (empty) contents will be inaccessible while still worn.
/// </summary>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SpillWhenWornComponent : Component
{
    /// <summary>
    /// Name of the solution to spill.
    /// </summary>
    [DataField]
    public string Solution = "default";

    /// <summary>
    /// Tracks if this item is currently being worn.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsWorn;
}
