using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers an entity when someone slipped on it.
/// The user is the entity that was slipped.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TriggerOnSlipComponent : BaseTriggerOnXComponent;
