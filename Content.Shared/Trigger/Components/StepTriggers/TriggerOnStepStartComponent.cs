using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.StepTriggers;

/// <inheritdoc cref="Systems.TriggerStepTriggeredOnEvent"/>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnStepStartComponent : BaseStepTriggerOnXComponent;
