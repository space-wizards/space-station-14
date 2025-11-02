using Robust.Shared.GameStates;

namespace Content.Shared.Light.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RandomPointLightComponent : Component
{
    [DataField, AutoNetworkedField]
    public float MaxRadius = 8;

    [DataField, AutoNetworkedField]
    public float MinRadius = 3;

    [DataField, AutoNetworkedField]
    public float MaxEnergy = 5;

    [DataField, AutoNetworkedField]
    public float MinEnergy = 0;
}
