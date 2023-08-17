using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class MovementBodyPartComponent : Component
{
    [DataField("walkSpeed")]
    public float WalkSpeed { get; private set; } = MovementSpeedModifierComponent.DefaultBaseWalkSpeed;

    [DataField("sprintSpeed")]
    public float SprintSpeed { get; private set; } = MovementSpeedModifierComponent.DefaultBaseSprintSpeed;

    [DataField("acceleration")]
    public float Acceleration = MovementSpeedModifierComponent.DefaultAcceleration;
}
