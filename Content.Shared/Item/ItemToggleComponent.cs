using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

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
    public bool Activated = false;

    /// <summary>
    ///     The noise this item makes when it is toggled on.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("soundActivate")]
    public SoundSpecifier? ActivateSound;

    /// <summary>
    ///     The noise this item makes when it is toggled off.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("soundDeactivate")]
    public SoundSpecifier? DeactivateSound;

    /// <summary>
    ///     The noise this item makes when it is toggled on.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("soundFailToActivate")]
    public SoundSpecifier? FailToActivateSound;

    /// <summary>
    ///     The noise this item makes when hitting something with it on.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("activatedSoundOnHit")]
    public SoundSpecifier? ActivatedSoundOnHit;

    /// <summary>
    ///     The noise this item makes when hitting something with it off.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("deactivatedSoundOnHit")]
    public SoundSpecifier? DeactivatedSoundOnHit;

    /// <summary>
    ///     The continuous noise this item makes when it's activated (like an e-sword's hum). This loops.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("activeSound")]
    public SoundSpecifier? ActiveSound;

    /// <summary>
    ///     The noise this item makes when swinging at nothing while activated.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("activatedSoundOnSwing")]
    public SoundSpecifier? ActivatedSoundOnSwing;

    /// <summary>
    ///     The noise this item makes when swinging at nothing while not activated.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("deactivatedSoundOnSwing")]
    public SoundSpecifier? DeactivatedSoundOnSwing;

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
    public DamageSpecifier? DeactivatedDamage = null;

    /// <summary>
    ///     Item's size when activated
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("activatedSize")]
    public ProtoId<ItemSizePrototype> ActivatedSize = "Huge";

    /// <summary>
    ///     Item's size when deactivated. If none is mentioned, it uses the item's default size instead.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("deactivatedSize")]
    public ProtoId<ItemSizePrototype>? DeactivatedSize = null;

    /// <summary>
    ///     Does this become hidden when deactivated
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("deactivatedSecret")]
    public bool DeactivatedSecret = false;

    /// <summary>
    ///     Item becomes hot when active.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("isHotWhenActivated")]
    public bool IsHotWhenActivated = false;

    /// <summary>
    ///     Item has this modifier to the chance to disarm when activated.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("activatedDisarmMalus")]
    public float? ActivatedDisarmMalus = null;

    /// <summary>
    ///     Item has this modifier to the chance to disarm when deactivated. If none is mentioned, it uses the item's default disarm modifier.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("DeactivatedDisarmMalus")]
    public float? DeactivatedDisarmMalus = null;

    /// <summary>
    ///     Item can be used to butcher when activated.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("activatedSharp")]
    public bool ActivatedSharp = false;

    /// <summary>
    ///     User entity used to store the information about the last user who has toggled the item. Used in other functions to affect the user in some way (like show messages).
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("user"), AutoNetworkedField]
    public EntityUid User { get; set; }

    /// <summary>
    ///     Used when the item emits sound while active.
    /// </summary>
    public EntityUid? Stream;
}

/// <summary>
/// Raised directed on an entity when its ItemToggle is attempted to be activated.
/// </summary>
[ByRefEvent]
public record struct ItemToggleActivateAttemptEvent()
{
    public bool Cancelled = false;
}

/// <summary>
/// Raised directed on an entity when activation changes have been applied on shared components. Used to call for server component changes.
/// </summary>
[ByRefEvent]
public readonly record struct ItemToggleActivatedServerChangesEvent;

/// <summary>
/// Raised directed on an entity when its ItemToggle is activated.
/// </summary>
[ByRefEvent]
public readonly record struct ItemToggleActivatedEvent;

/// <summary>
/// Raised directed on an entity when its ItemToggle is attempted to be deactivated.
/// </summary>
[ByRefEvent]
public record struct ItemToggleDeactivateAttemptEvent()
{
    public bool Cancelled = false;
}

/// <summary>
/// Raised directed on an entity when deactivation changes have been applied on shared components. Used to call for server component changes.
/// </summary>
[ByRefEvent]
public readonly record struct ItemToggleDeactivatedServerChangesEvent;

/// <summary>
/// Raised directed on an entity when its ItemToggle is deactivated.
/// </summary>
[ByRefEvent]
public readonly record struct ItemToggleDeactivatedEvent;

/// <summary>
/// Raised directed on an entity when another component forces a toggle (like running out of battery).
/// </summary>
[ByRefEvent]
public readonly record struct ItemToggleForceToggleEvent;


