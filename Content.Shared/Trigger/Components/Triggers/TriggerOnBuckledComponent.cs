using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers when the the component parent is buckled.
/// </summary>

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnBuckledComponent : BaseTriggerOnXComponent;
