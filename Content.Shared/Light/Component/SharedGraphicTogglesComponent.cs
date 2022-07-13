using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Light.Component;

/// <summary>
///     Give mob ability to see in complete darkness.
/// </summary>
[Virtual]
public class SharedGraphicTogglesComponent : Robust.Shared.GameObjects.Component
{
}

public sealed class ToggleFoVActionEvent : InstantActionEvent {}
public sealed class ToggleShadowsActionEvent : InstantActionEvent {}
public sealed class ToggleLightingActionEvent : InstantActionEvent {}
