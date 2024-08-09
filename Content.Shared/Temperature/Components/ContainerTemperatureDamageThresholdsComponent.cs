using Robust.Shared.GameStates;

namespace Content.Shared.Temperature.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ContainerTemperatureDamageThresholdsComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public float? HeatDamageThreshold;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public float? ColdDamageThreshold;
}
