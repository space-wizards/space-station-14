using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

/// <summary>
/// Added to someone using a jetpack for movement purposes
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class JetpackUserComponent : Component
{
    public float Acceleration = 1f;
    public float Friction = 0.3f;
    public float WeightlessModifier = 1.2f;
}
