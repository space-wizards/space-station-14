using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Silicons.StationAi;

/// <summary>
/// Holds data for customizing the appearance of station AIs.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StationAiCustomizationComponent : Component
{
    /// <summary>
    /// The proto ID of the layer data for customizing the station AI's core.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<StationAiCustomizationPrototype>? StationAiCoreLayerData = null;

    /// <summary>
    /// The proto ID of the layer data for customizing the station AI's hologram.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<StationAiCustomizationPrototype>? StationAiHologramLayerData = null;
}

[Serializable, NetSerializable]
public enum StationAiCustomization : byte
{
    Core,
    Hologram,
}
