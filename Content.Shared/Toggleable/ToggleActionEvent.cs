using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared.Toggleable;

/// <summary>
/// Generic action-event for toggle-able components.
/// If you are using <c>ItemToggleComponent</c> subscribe to <c>ItemToggledEvent</c> instead.
/// </summary>
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
