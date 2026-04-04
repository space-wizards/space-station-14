using Robust.Shared.GameStates;

namespace Content.Shared.SurveillanceCamera.Components;

/// <summary>
/// Can activate <see cref="CameraActiveOnCollideComponent"/> when collided with.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CameraActiveOnCollideColliderComponent : Component
{
    [DataField]
    public string FixtureId = "lightTrigger";
}
