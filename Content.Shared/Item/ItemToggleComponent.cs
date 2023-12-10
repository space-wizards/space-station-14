using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Item;

/// <summary>
/// Handles generic item toggles, like a welder turning on and off, or an e-sword.
/// </summary>
/// <remarks>
/// If you need extended functionality (e.g. requiring power) then add a new component and use events.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ItemToggleComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("activated"), AutoNetworkedField]
    public bool Activated;

    /// <summary>
    ///     The noise this item makes when it is toggled on.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("soundActivate"), AutoNetworkedField]
    public SoundSpecifier? ActivateSound;

    /// <summary>
    ///     The noise this item makes when it is toggled off.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("soundDeactivate"), AutoNetworkedField]
    public SoundSpecifier? DeactivateSound;

    /// <summary>
    ///     The noise this item makes when hitting something with it on.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("activatedSoundOnHit"), AutoNetworkedField]
    public SoundSpecifier? ActivatedSoundOnHit;

    /// <summary>
    ///     The noise this item makes when hitting something with it off.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("deactivatedSoundOnHit"), AutoNetworkedField]
    public SoundSpecifier? DeactivatedSoundOnHit;

    /// <summary>
    ///     The noise this item makes when hitting something with it off.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("activeSound"), AutoNetworkedField]
    public SoundSpecifier? ActiveSound;

    /// <summary>
    ///     The noise this item makes when swinging at nothing while activated.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("activatedSoundOnSwing"), AutoNetworkedField]
    public SoundSpecifier ActivatedSoundOnSwing = new SoundPathSpecifier("/Audio/Weapons/punchmiss.ogg");

    /// <summary>
    ///     The noise this item makes when swinging at nothing while not activated.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("deactivatedSoundOnSwing"), AutoNetworkedField]
    public SoundSpecifier DeactivatedSoundOnSwing = new SoundPathSpecifier("/Audio/Weapons/punchmiss.ogg");

    /// <summary>
    ///     Damage done by this item when activated.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("activatedDamage")]
    public DamageSpecifier ActivatedDamage = new();

    /// <summary>
    ///     Damage done by this item when deactivated.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("deactivatedDamage")]
    public DamageSpecifier DeactivatedDamage = new();


    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("activatedDisarmMalus")]
    public float ActivatedDisarmMalus = 0.6f;


    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("activatedSharp")]
    public bool ActivatedSharp = false;

    /// <summary>
    ///     Does this become hidden when deactivated
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("deactivatedSecret")]
    public bool DeactivatedSecret = false;

    /// <summary>
    ///     Item's size increase when activated
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("activatedSizeModifier"), AutoNetworkedField]
    public int ActivatedSizeModifier = 0;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("isHotWhenActivated"), AutoNetworkedField]
    public bool IsHotWhenActivated = false;

    /// <summary>
    ///     Used when the item emits sound while active.
    /// </summary>
    public IPlayingAudioStream? Stream;
}

/// <summary>
/// Raised directed on an entity when its ItemToggle is attempted to be activated.
/// </summary>
[ByRefEvent]
public readonly record struct ItemToggleActivateAttemptEvent(bool Cancelled = false);

/// <summary>
/// Raised directed on an entity when its ItemToggle is activated.
/// </summary>
[ByRefEvent]
public readonly record struct ItemToggleActivatedEvent;

/// <summary>
/// Raised directed on an entity when its ItemToggle is attempted to be deactivated.
/// </summary>
[ByRefEvent]
public readonly record struct ItemToggleDeactivateAttemptEvent(bool Cancelled = false);

/// <summary>
/// Raised directed on an entity when its ItemToggle is deactivated.
/// </summary>
[ByRefEvent]
public readonly record struct ItemToggleDeactivatedEvent;


