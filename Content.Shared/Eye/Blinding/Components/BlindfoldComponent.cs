using Robust.Shared.GameStates;

namespace Content.Shared.Eye.Blinding.Components;

/// <summary>
///     Blinds a person when an item with this component is equipped to the eye, head, or mask slot.
/// </summary>
[RegisterComponent]
[NetworkedComponent]
public sealed partial class BlindfoldComponent : Component
{
    /// <summary>
    ///   Determines if this component is allowed to blind someone.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("enabled")]
    public bool Enabled { get; set; } = true;

}
