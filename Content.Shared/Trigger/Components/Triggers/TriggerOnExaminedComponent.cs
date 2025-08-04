using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers when the receiving object/entity/thing is examined.
/// Triggers on the thing being examined, not on the one doing the examining.
/// </summary>

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnExaminedComponent : BaseTriggerOnXComponent;
