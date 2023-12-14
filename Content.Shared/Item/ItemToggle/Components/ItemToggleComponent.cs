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



