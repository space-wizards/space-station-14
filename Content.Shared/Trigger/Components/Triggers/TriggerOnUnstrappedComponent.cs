using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers when the component parent is unstrapped.
/// </summary>

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnUnstrappedComponent : BaseTriggerOnXComponent;
