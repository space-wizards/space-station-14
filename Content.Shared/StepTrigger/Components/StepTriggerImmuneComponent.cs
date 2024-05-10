using Robust.Shared.GameStates;

namespace Content.Shared.StepTrigger.Components;

/// <summary>
/// Grants the attached entity to step triggers.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StepTriggerImmuneComponent : Component;
