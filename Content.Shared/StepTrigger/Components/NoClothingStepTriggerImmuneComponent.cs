using Robust.Shared.GameStates;

namespace Content.Shared.StepTrigger.Components;

/// <summary>
/// Grants the attached entity immunity to  without needing clothes.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NoClothingStepTriggerImmuneComponent : Component;
