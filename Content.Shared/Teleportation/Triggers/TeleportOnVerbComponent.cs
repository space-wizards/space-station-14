using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Teleportation.Triggers;

/// <summary>
/// Adds a teleport verb with configurable user restrictions. Activation requests teleportation.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TeleportOnVerbComponent : Component
{
    /// <summary>
    /// Whether this verb requests effects before and after teleportation.
    /// Use checks and the successful teleport notification are unaffected.
    /// </summary>
    [DataField]
    public bool ShouldTriggerEffects = true;

    /// <summary>
    /// Determines the verb menu and shortcut used to activate this trigger.
    /// </summary>
    [DataField]
    public TeleportVerbType VerbType;

    /// <summary>
    /// Require the user to pass the standard interaction blockers before offering this verb.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RequireCanInteract = true;

    /// <summary>
    /// Require the user to have a HandsComponent. Does not check hand count or whether a hand is empty.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RequireHands = true;

    /// <summary>
    /// Text displayed for the teleport verb.
    /// </summary>
    [DataField(required: true)]
    public LocId VerbText;

    /// <summary>
    /// Optional icon displayed for the verb.
    /// </summary>
    [DataField]
    public SpriteSpecifier? VerbIcon;

    /// <summary>
    /// Optional localization key for the verb's category name.
    /// </summary>
    [DataField]
    public LocId? VerbCategory;

    /// <summary>
    /// Ordering within the selected verb type. Higher priorities appear first.
    /// </summary>
    [DataField]
    public int Priority;

    /// <summary>
    /// Description displayed while the teleport verb is available.
    /// </summary>
    [DataField]
    public LocId? EnabledMessage;

    /// <summary>
    /// Message shown for an unavailable verb if the teleport check supplied no cancellation reason.
    /// Displayed only when <see cref="HideWhenDisabled"/> is false.
    /// </summary>
    [DataField]
    public LocId? DisabledMessage;

    /// <summary>
    /// Hide unavailable verbs instead of displaying them disabled.
    /// </summary>
    [DataField]
    public bool HideWhenDisabled;

    /// <summary>
    /// Entities allowed to use the teleport verb.
    /// </summary>
    [DataField]
    public EntityWhitelist? UserWhitelist;

    /// <summary>
    /// Entities prevented from using the teleport verb.
    /// </summary>
    [DataField]
    public EntityWhitelist? UserBlacklist;
}

/// <summary>
/// Selects the verb menu and shortcut used to activate a teleporter.
/// </summary>
[Serializable, NetSerializable]
public enum TeleportVerbType : byte
{
    Verb,
    Alternative,
    Interaction,
    Activation,
}
