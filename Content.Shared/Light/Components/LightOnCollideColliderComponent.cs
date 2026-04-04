using Robust.Shared.GameStates;

namespace Content.Shared.Light.Components;

/// <summary>
/// Can activate <see cref="CameraLightOnCollideComponent"/> when collided with.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CameraLightOnCollideColliderComponent : Component
{
    [DataField]
    public string FixtureId = "lightTrigger";
}
