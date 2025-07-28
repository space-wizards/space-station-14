using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.StepTriggers;

/// <inheritdoc cref="Systems.TriggerStepTriggeredOffEvent"/>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TriggerOnStepEndComponent : BaseStepTriggerOnXComponent;
