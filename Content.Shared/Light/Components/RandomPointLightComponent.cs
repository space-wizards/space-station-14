using Robust.Shared.GameStates;

namespace Content.Shared.Light.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RandomPointLightComponent : Component
{
    [DataField, AutoNetworkedField]
    public float MaxRadius = 6;

    [DataField, AutoNetworkedField]
    public float MinRadius = 1;

    [DataField, AutoNetworkedField]
    public float MaxEnergy = 5;

    [DataField, AutoNetworkedField]
    public float MinEnergy = 1;
}
