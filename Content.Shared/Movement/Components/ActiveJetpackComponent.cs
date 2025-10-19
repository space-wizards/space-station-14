using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared.Movement.Components;

/// <summary>
/// Added to an enabled jetpack. Tracks gas usage on server / effect spawning on client.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ActiveJetpackComponent : Component
{
    public float EffectCooldown = 0.3f;

    public float MaxDistance = 0.7f;

    public EntityCoordinates LastCoordinates;

    public TimeSpan TargetTime = TimeSpan.Zero;
}
