using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._MACRO.CombatMode;

/// <summary>
/// Allows the use of unique sprites for combat mode
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CombatModeVisualsComponent : Component
{
    /// <summary>
    /// Sprite to set for the combat layer
    /// </summary>
    [DataField(required: true), ViewVariables(VVAccess.ReadOnly)]
    public SpriteSpecifier Sprite;

    /// <summary>
    /// Hides the base layer when combat mode is activated if it exists
    /// </summary>
    [DataField]
    public bool HideBaseLayer;
}
