using Robust.Shared.GameStates;

namespace Content.Shared.Photography;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CameraComponent : Component
{
    [DataField, AutoNetworkedField]
    public float ImageSize = 3f;
    [DataField, AutoNetworkedField]
    public int TargetWidth = 3;
}
