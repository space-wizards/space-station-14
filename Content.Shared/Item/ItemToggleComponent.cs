using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Item;

/// <summary>
/// Handles generic item toggles, like a welder turning on and off, or an e-sword.
/// </summary>
/// <remarks>
/// If you need extended functionality (e.g. requiring power) then add a new component and use events:
/// ItemToggleActivateAttemptEvent, ItemToggleDectivateAttemptEvent or ItemToggleForceToggleEvent.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ItemToggleComponent : Component
{
    /// <summary>
    ///     The toggle state the item starts at.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public bool Activated = false;

    /// <summary>
    ///     The noise this item makes when it is toggled on.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public SoundSpecifier? SoundActivate;

    /// <summary>
    ///     The noise this item makes when it is toggled off.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public SoundSpecifier? SoundDeactivate;

    /// <summary>
    ///     The noise this item makes when it is toggled on.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public SoundSpecifier? SoundFailToActivate;

    /// <summary>
    ///     The noise this item makes when hitting something with it on.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public SoundSpecifier? ActivatedSoundOnHit;

    /// <summary>
    ///     The noise this item makes when hitting something with it off.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public SoundSpecifier? DeactivatedSoundOnHit;

    /// <summary>
    ///     The continuous noise this item makes when it's activated (like an e-sword's hum). This loops.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public SoundSpecifier? ActiveSound;

    /// <summary>
    ///     The noise this item makes when swinging at nothing while activated.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public SoundSpecifier? ActivatedSoundOnSwing;

    /// <summary>
    ///     The noise this item makes when swinging at nothing while not activated.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public SoundSpecifier? DeactivatedSoundOnSwing;

    /// <summary>
    ///     Damage done by this item when activated.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public DamageSpecifier ActivatedDamage = new();

    /// <summary>
    ///     Damage done by this item when deactivated.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public DamageSpecifier? DeactivatedDamage = null;

    /// <summary>
    ///     Item's size when activated
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public ProtoId<ItemSizePrototype> ActivatedSize = "Huge";

    /// <summary>
    ///     Item's size when deactivated. If none is mentioned, it uses the item's default size instead.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public ProtoId<ItemSizePrototype>? DeactivatedSize = null;

    /// <summary>
    ///     Does this become hidden when deactivated
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public bool DeactivatedSecret = false;

    /// <summary>
    ///     Item becomes hot when active.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public bool IsHotWhenActivated = false;

    /// <summary>
    ///     Item has this modifier to the chance to disarm when activated.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float? ActivatedDisarmMalus = null;

    /// <summary>
    ///     Item has this modifier to the chance to disarm when deactivated. If none is mentioned, it uses the item's default disarm modifier.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float? DeactivatedDisarmMalus = null;

    /// <summary>
    ///     Item can be used to butcher when activated.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public bool ActivatedSharp = false;

    /// <summary>
    ///     Used when the item emits sound while active.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? PlayingStream;
}

/// <summary>
/// Raised directed on an entity when its ItemToggle is attempted to be activated.
/// </summary>
[ByRefEvent]
public record struct ItemToggleActivateAttemptEvent(EntityUid? User)
{
    public bool Cancelled = false;
    public EntityUid? User { get; set; } = User;
}

/// <summary>
/// Raised directed on an entity when its ItemToggle is activated.
/// </summary>
[ByRefEvent]
public readonly record struct ItemToggleActivatedEvent;

/// <summary>
/// Raised directed on an entity when its ItemToggle is attempted to be deactivated.
/// </summary>
[ByRefEvent]
public record struct ItemToggleDeactivateAttemptEvent(EntityUid? User)
{
    public bool Cancelled = false;
    public EntityUid? User { get; set; } = User;
}

/// <summary>
/// Raised directed on an entity when its ItemToggle is deactivated.
/// </summary>
[ByRefEvent]
public readonly record struct ItemToggleDeactivatedEvent;

/// <summary>
/// Raised directed on an entity when another component forces a toggle (like running out of battery).
/// </summary>
[ByRefEvent]
public record struct ItemToggleForceToggleEvent(EntityUid? User)
{
    public EntityUid? User { get; set; } = User;
}



