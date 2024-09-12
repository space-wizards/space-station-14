using Content.Shared.Weapons.Melee;
using Robust.Shared.GameStates;

namespace Content.Server.Revenant.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class RevenantAnimatedComponent : Component
{
    /// <summary>
    /// The MeleeWeaponComponent that was added when this item was animated, which
    /// will be deleted when the item goes inanimate.
    /// If the animated item already had a MeleeWeaponComponent, this will be null.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public MeleeWeaponComponent? AddedMelee;
}