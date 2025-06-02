using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Silicons.StationAi;

/// <summary>
/// Holds data for altering the appearance of station AIs.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StationAiCustomizationComponent : Component
{
    /// <summary>
    /// Dictionary of the prototype data used for customizing the appearance of the entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<StationAiCustomizationGroupPrototype>, ProtoId<StationAiCustomizationPrototype>> ProtoIds = new();
}

/// <summary>
/// Message sent to server that contains a station AI customization that the client has selected
/// </summary>
[Serializable, NetSerializable]
public sealed class StationAiCustomizationMessage : BoundUserInterfaceMessage
{
    public readonly ProtoId<StationAiCustomizationGroupPrototype> GroupProtoId;
    public readonly ProtoId<StationAiCustomizationPrototype> CustomizationProtoId;

    public StationAiCustomizationMessage(ProtoId<StationAiCustomizationGroupPrototype> groupProtoId, ProtoId<StationAiCustomizationPrototype> customizationProtoId)
    {
        GroupProtoId = groupProtoId;
        CustomizationProtoId = customizationProtoId;
    }
}

/// <summary>
/// Key for opening the station AI customization UI
/// </summary>
[Serializable, NetSerializable]
public enum StationAiCustomizationUiKey : byte
{
    Key,
}

/// <summary>
/// The different catagories of station Ai customizations available
/// </summary>
[Serializable, NetSerializable]
public enum StationAiCustomizationType : byte
{
    CoreIconography,
    Hologram,
}
