using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.StepTriggers;

/// <summary>
/// Makes step attempt always continue.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnStepAlwaysAttemptComponent : BaseStepTriggerOnXComponent;
