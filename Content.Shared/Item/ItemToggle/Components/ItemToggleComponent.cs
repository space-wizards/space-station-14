using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Item.ItemToggle.Components;

/// <summary>
/// Handles generic item toggles, like a welder turning on and off, or an e-sword.
/// </summary>
/// <remarks>
/// If you need extended functionality (e.g. requiring power) then add a new component and use events:
/// ItemToggleActivateAttemptEvent, ItemToggleDeactivateAttemptEvent, ItemToggledEvent.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ItemToggleComponent : Component
{
    /// <summary>
    ///     The item's toggle state.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Activated = false;

    /// <summary>
    /// Can the entity be activated in the world.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool OnActivate = true;

    /// <summary>
    /// If this is set to false then the item can't be toggled by pressing Z.
    /// Use another system to do it then.
    /// </summary>
    [DataField]
    public bool OnUse = true;

    /// <summary>
    ///     The localized text to display in the verb to activate.
    /// </summary>
    [DataField]
    public string VerbToggleOn = "item-toggle-activate";

    /// <summary>
    ///     The localized text to display in the verb to de-activate.
    /// </summary>
    [DataField]
    public string VerbToggleOff = "item-toggle-deactivate";

    /// <summary>
    ///     Whether the item's toggle can be predicted by the client.
    /// </summary>
    /// /// <remarks>
    /// If server-side systems affect the item's toggle, like charge/fuel systems, then the item is not predictable.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public bool Predictable = true;

    /// <summary>
    ///     The noise this item makes when it is toggled on.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? SoundActivate;

    /// <summary>
    ///     The noise this item makes when it is toggled off.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? SoundDeactivate;

    /// <summary>
    ///     The popup to show to someone activating this item.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId? PopupActivate;

    /// <summary>
    ///     The popup to show to someone deactivating this item.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId? PopupDeactivate;

    /// <summary>
    ///     The noise this item makes when it is toggled on.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? SoundFailToActivate;
}

/// <summary>
/// Raised directed on an entity when its ItemToggle is attempted to be activated.
/// </summary>
[ByRefEvent]
public record struct ItemToggleActivateAttemptEvent(EntityUid? User)
{
    /// <summary>
    /// Should we silently fail.
    /// </summary>
    public bool Silent = false;

    public bool Cancelled = false;
    public readonly EntityUid? User = User;

    /// <summary>
    /// Pop-up that gets shown to users explaining why the attempt was cancelled.
    /// </summary>
    public string? Popup;
}

/// <summary>
/// Raised directed on an entity when its ItemToggle is attempted to be deactivated.
/// </summary>
[ByRefEvent]
public record struct ItemToggleDeactivateAttemptEvent(EntityUid? User)
{
    /// <summary>
    /// Should we silently fail.
    /// </summary>
    public bool Silent = false;

    public bool Cancelled = false;
    public readonly EntityUid? User = User;

    /// <summary>
    /// Pop-up that gets shown to users explaining why the attempt was cancelled.
    /// </summary>
    public string? Popup;
}

/// <summary>
/// Raised directed on an entity any sort of toggle is complete.
/// </summary>
[ByRefEvent]
public readonly record struct ItemToggledEvent(bool Predicted, bool Activated, EntityUid? User)
{
    public readonly bool Predicted = Predicted;
    public readonly bool Activated = Activated;
    public readonly EntityUid? User = User;
}
