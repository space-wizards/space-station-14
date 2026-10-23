using Robust.Shared.GameStates;

namespace Content.Shared.UserInterface;

/// <summary>
/// Specifies the entity as requiring anchoring to keep the ActivatableUI open.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ActivatableUIRequiresAnchorComponent : Component
{
    [DataField]
    public LocId? Popup = "ui-needs-anchor";
}
