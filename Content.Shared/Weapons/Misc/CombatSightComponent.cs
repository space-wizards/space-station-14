using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Weapons.Misc;

/// <summary>
///     Lets weapons change the reticle around the mouse when held
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CombatSightComponent : Component
{
    /// <summary>
    ///     The sight used when someone holds this item in combat mode
    /// </summary>
    [DataField, AutoNetworkedField]
    public SpriteSpecifier? Sight;

    /// <summary>
    ///     The sight used when someone is holding this item in combat mode,
    ///     but it cannot be used at the moment
    ///     Currently only used when a gun is bolted.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SpriteSpecifier? Unavailable;
}
