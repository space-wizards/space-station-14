using Robust.Shared.GameStates;

namespace Content.Shared.Light.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TileEmissionComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Range = 0.25f;

    [DataField(required: true), AutoNetworkedField]
    public Color Color = Color.Transparent;
}
