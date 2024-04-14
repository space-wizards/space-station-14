using Content.Shared.StepTrigger.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.StepTrigger.Components;

/// <summary>
/// This is used for cancelling step trigger events if the user is wearing shoes, such as for glass shards.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ShoesRequiredStepTriggerSystem))]
public sealed partial class ShoesRequiredStepTriggerImmuneComponent : Component
{
}
