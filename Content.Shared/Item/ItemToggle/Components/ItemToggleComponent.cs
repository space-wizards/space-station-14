using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Item;

/// <summary>
/// Handles generic item toggles, like a welder turning on and off, or an e-sword.
/// </summary>
/// <remarks>
/// If you need extended functionality (e.g. requiring power) then add a new component and use events:
/// ItemToggleActivateAttemptEvent, ItemToggleDeactivateAttemptEvent or ItemToggleForceToggleEvent.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ItemToggleComponent : Component
{
    /// <summary>
    ///     The item's toggle state.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public bool Activated = false;

    /// <summary>
    ///     Whether the item's toggle can be predicted by the client.
    /// </summary>
    /// /// <remarks>
    /// If server-side systems affect the item's toggle, like charge/fuel systems, then the item is not predictable.
    /// </remarks>
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public bool Predictable = true;

    /// <summary>
    ///     The noise this item makes when it is toggled on.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public SoundSpecifier? SoundActivate;

    /// <summary>
    ///     The noise this item makes when it is toggled off.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public SoundSpecifier? SoundDeactivate;

    /// <summary>
    ///     The noise this item makes when it is toggled on.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public SoundSpecifier? SoundFailToActivate;
}

/// <summary>
/// Raised directed on an entity when its ItemToggle is attempted to be activated.
/// </summary>
[ByRefEvent]
public record struct ItemToggleActivateAttemptEvent(EntityUid? User)
{
    public bool Cancelled = false;
    public EntityUid? User = User;
}

/// <summary>
/// Raised directed on an entity when its ItemToggle is attempted to be deactivated.
/// </summary>
[ByRefEvent]
public record struct ItemToggleDeactivateAttemptEvent(EntityUid? User)
{
    public bool Cancelled = false;
    public EntityUid? User = User;
}

/// <summary>
/// Raised directed on an entity any sort of toggle is complete.
/// </summary>
[ByRefEvent]
public record struct ItemToggleDoneEvent(bool Activated, EntityUid? User)
{
    public bool Activated = Activated;
    public EntityUid? User = User;
}

/// <summary>
/// Raised in order to play a toggle sound effect.
/// </summary>
[ByRefEvent]
public record struct ItemTogglePlayToggleSoundEvent(bool Activated, bool Predicted, EntityUid? User)
{
    public bool Activated = Activated;
    public bool Predicted = Predicted;
    public EntityUid? User = User;
}

/// <summary>
/// Raised in order to play a failure to toggle sound effect.
/// </summary>
[ByRefEvent]
public record struct ItemTogglePlayFailSoundEvent(bool Predicted, EntityUid? User)
{
    public bool Predicted = Predicted;
    public EntityUid? User = User;
}

/// <summary>
/// Raised in order to effect changes upon the Light component of the entity.
/// </summary>
[ByRefEvent]
public record struct ItemToggleLightUpdateEvent(bool Activated)
{
    public bool Activated = Activated;
}

/// <summary>
/// Raised in order to effect changes upon the Appearance component of the entity.
/// </summary>
[ByRefEvent]
public record struct ItemToggleAppearanceUpdateEvent(bool Activated)
{
    public bool Activated = Activated;
}

/// <summary>
/// Raised in order to effect changes upon the Reflect component of the entity.
/// </summary>
[ByRefEvent]
public record struct ItemToggleReflectUpdateEvent(bool Activated)
{
    public bool Activated = Activated;
}
