using System.Numerics;

namespace Content.Server.Movement.Components;

[RegisterComponent]
public sealed partial class StressTestMovementComponent : Component
{
    public float Progress { get; set; }
    public Vector2 Origin { get; set; }
}
