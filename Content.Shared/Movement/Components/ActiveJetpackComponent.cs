using Content.Shared.Movement.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared.Movement.Components;

/// <summary>
/// Added to an enabled jetpack. Tracks gas usage on server / effect spawning on client.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedJetpackSystem))]
public sealed partial class ActiveJetpackComponent : Component
{
    public EntityCoordinates LastCoordinates;

    public TimeSpan TargetTime = TimeSpan.Zero;
}
