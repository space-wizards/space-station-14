using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.StepTriggers;

/// <summary>
/// This is used for marking step trigger events that require the user
/// to wear shoes or have protection of some sort, such as for glass shards.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerPreventableStepTriggerComponent : BaseStepTriggerOnXComponent;
