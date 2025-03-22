using Robust.Shared.GameStates;

namespace Content.Shared.Temperature.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ContainerTemperatureDamageThresholdsComponent: Component
{
    [DataField, AutoNetworkedField]
    public float? HeatDamageThreshold;

    [DataField, AutoNetworkedField]
    public float? ColdDamageThreshold;
}
