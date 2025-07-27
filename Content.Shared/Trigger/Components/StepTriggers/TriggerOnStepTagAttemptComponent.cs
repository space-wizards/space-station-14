using Content.Shared.Trigger.Components.Triggers;
using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.StepTriggers;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnStepTagAttemptComponent : BaseTriggerOnXComponent;
