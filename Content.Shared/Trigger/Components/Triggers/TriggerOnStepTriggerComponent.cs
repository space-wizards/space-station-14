using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Triggers;

/// <summary>
/// Triggers if a StepTrigger is activated by someone stepping on this entity.
/// The user is the mob who stepped on it.
/// </summary>
/// <remarks>
/// This is used for entities that want the more generic 'trigger' behavior after a step trigger occurs.
/// Not done by default, since it's not useful for everything and might cause weird behavior. But it is useful for a lot of stuff like mousetraps.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnStepTriggerComponent : BaseTriggerOnXComponent;
