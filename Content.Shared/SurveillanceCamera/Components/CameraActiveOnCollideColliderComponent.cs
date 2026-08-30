using Robust.Shared.GameStates;

namespace Content.Shared.SurveillanceCamera.Components;

/// <summary>
/// Can activate <see cref="CameraActiveOnCollideComponent"/> when collided with.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CameraActiveOnCollideColliderComponent : Component
{
    /// <summary>
    /// The fixture id used for detecting the collision.
    /// </summary>
    [DataField]
    public string FixtureId = "lightTrigger";
}
