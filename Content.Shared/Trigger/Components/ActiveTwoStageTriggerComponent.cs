using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components;

/// <summary>
/// Component used for tracking active two-stage triggers.
/// Used internally for performance reasons.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ActiveTwoStageTriggerComponent : Component;
