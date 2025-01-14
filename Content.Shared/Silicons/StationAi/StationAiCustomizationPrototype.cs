using Robust.Shared.Prototypes;

namespace Content.Shared.Silicons.StationAi;

/// <summary>
/// Holds data for customizing the appearance of station AIs.
/// </summary>
[Prototype]
public sealed partial class StationAiCustomizationPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = string.Empty;

    /// <summary>
    /// Stores data which is used to modify the appearance of station AI.
    /// </summary>
    [DataField]
    public Dictionary<string, PrototypeLayerData> LayerData = new();
}
