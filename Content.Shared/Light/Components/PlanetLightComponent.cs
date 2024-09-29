using Robust.Shared.GameStates;

namespace Content.Shared.Light.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PlanetLightComponent : Component
{
    [DataField, AutoNetworkedField]
    public Color Color = Color.Black;
}
