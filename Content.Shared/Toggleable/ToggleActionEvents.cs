using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared.Toggleable;

/// <summary>
///     Raised directed on an entity with <see cref="ToggleableComponent"/> when it is set try and be enabled.
///     Should set the <see cref="ToggleableComponent.Enabled"/> variable of the entity's <see cref="ToggleableComponent"/> when being handled.
/// </summary>
/// <remarks>
///     Expect that this may be handled by multiple systems at once.
/// </remarks>
[ByRefEvent]
public readonly record struct ToggleableEnabledEvent;

/// <summary>
///     Raised directed on an entity with <see cref="ToggleableComponent"/> when it is set try and be enabled.
///     Should set the <see cref="ToggleableComponent.Enabled"/> variable of the entity's <see cref="ToggleableComponent"/> when being handled.
/// </summary>
/// <remarks>
///     Expect that this may be handled by multiple systems at once.
/// </remarks>
[ByRefEvent]
public readonly record struct ToggleableDisabledEvent;

/// <summary>
/// Generic action-event for toggle-able components.
/// </summary>
/// <remarks>
/// If you are using <c>ItemToggleComponent</c>, subscribe to <c>ItemToggledEvent</c> instead.
/// </remarks>
public sealed partial class ToggleActionEvent : InstantActionEvent;

/// <summary>
///     Generic enum keys for toggle-visualizer appearance data & sprite layers.
/// </summary>
[Serializable, NetSerializable]
public enum ToggleVisuals : byte
{
    Toggled,
    Layer
}
