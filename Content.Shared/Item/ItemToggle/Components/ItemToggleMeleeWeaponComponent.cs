using Robust.Shared.GameStates;
using Robust.Shared.Audio;
using Content.Shared.Damage;

namespace Content.Shared.Item;

/// <summary>
/// Handles the changes to the melee weapon component when the item is toggled. 
/// </summary>
/// <remarks>
/// You can change the damage, sound on hit, on swing, as well as hidden status while activated.
/// </remarks>
[RegisterComponent, NetworkedComponent]
public sealed partial class ItemToggleMeleeWeaponComponent : Component
{
    /// <summary>
    ///     The noise this item makes when hitting something with it on.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public SoundSpecifier? ActivatedSoundOnHit;

    /// <summary>
    ///     The noise this item makes when hitting something with it off.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public SoundSpecifier? DeactivatedSoundOnHit;

    /// <summary>
    ///     The noise this item makes when hitting something with it on and it does no damage.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public SoundSpecifier? ActivatedSoundOnHitNoDamage;

    /// <summary>
    ///     The noise this item makes when hitting something with it off and it does no damage.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public SoundSpecifier? DeactivatedSoundOnHitNoDamage;

    /// <summary>
    ///     The noise this item makes when swinging at nothing while activated.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public SoundSpecifier? ActivatedSoundOnSwing;

    /// <summary>
    ///     The noise this item makes when swinging at nothing while not activated.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public SoundSpecifier? DeactivatedSoundOnSwing;

    /// <summary>
    ///     Damage done by this item when activated.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public DamageSpecifier? ActivatedDamage = null;

    /// <summary>
    ///     Damage done by this item when deactivated.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public DamageSpecifier? DeactivatedDamage = null;

    /// <summary>
    ///     Does this become hidden when deactivated
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public bool DeactivatedSecret = false;
}

/// <summary>
/// Raised in order to effect changes upon the MeleeWeaponComponent of the entity.
/// </summary>
[ByRefEvent]
public record struct ItemToggleMeleeWeaponUpdateEvent(bool Activated)
{
    public bool Activated = Activated;
}

