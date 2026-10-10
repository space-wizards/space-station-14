using Content.Shared.Atmos.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Components;

[NetworkedComponent]
public abstract partial class SharedMapAtmosphereComponent : Component
{
    [ViewVariables] public SharedGasTileOverlaySystem.SharedFireData FireData;
    [ViewVariables] public SharedGasTileOverlaySystem.SharedVisibleGasData VisibleGasData;
    [ViewVariables] public SharedGasTileOverlaySystem.SharedGasTemperatureData GasTemperatureData;
}

[Serializable, NetSerializable]
public sealed class MapAtmosphereComponentState : ComponentState
{
    public SharedGasTileOverlaySystem.SharedFireData SharedFireData;
    public SharedGasTileOverlaySystem.SharedVisibleGasData SharedVisibleGasData;
    public SharedGasTileOverlaySystem.SharedGasTemperatureData SharedGasTemperatureData;

    public MapAtmosphereComponentState(SharedGasTileOverlaySystem.SharedFireData sharedFireData, SharedGasTileOverlaySystem.SharedVisibleGasData sharedVisibleGasData, SharedGasTileOverlaySystem.SharedGasTemperatureData sharedGasTemperatureData)
    {
        SharedFireData = sharedFireData;
        SharedVisibleGasData = sharedVisibleGasData;
        SharedGasTemperatureData = sharedGasTemperatureData;
    }
}
