using Robust.Shared.GameStates;

namespace Content.Shared.SurveillanceCamera.Components;

/// <summary>
/// Marks an entity with <see cref="SurveillanceCameraComponent"/> whenever entities are contacting with it.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CameraActiveOnCollideComponent : Component
{
    /// <summary>
    /// Whether this camera is currently being collided with.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled;

    /// <summary>
    /// Whether the entity must be powered for this component to work.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RequiresPower = true;
}
