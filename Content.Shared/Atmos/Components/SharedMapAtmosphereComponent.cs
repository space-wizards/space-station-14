using Content.Shared.Atmos.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Components;

[NetworkedComponent]
public abstract partial class SharedMapAtmosphereComponent : Component
{
    [ViewVariables] public SharedGasTileOverlaySystem.GasOverlayData OverlayData;
}

[Serializable, NetSerializable]
public sealed class MapAtmosphereComponentState : ComponentState
{
    public SharedGasTileOverlaySystem.GasOverlayData Overlay;

    public MapAtmosphereComponentState(SharedGasTileOverlaySystem.GasOverlayData overlay)
    {
        Overlay = overlay;
    }
}
