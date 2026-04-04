using Robust.Shared.GameStates;

namespace Content.Shared.Light.Components;

/// <summary>
/// Enables / disables pointlight whenever entities are contacting with it
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class CameraLightOnCollideComponent : Component
{
    /// <summary>
    /// Whether this camera light is currently being collided with.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled;
}
