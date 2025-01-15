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
    /// The (unlocalized) name of the customization.
    /// </summary>
    [DataField]
    public string Name = string.Empty;

    /// <summary>
    /// The type of customization.
    /// </summary>
    [DataField]
    public StationAiCustomizationType Category = StationAiCustomizationType.Core;

    /// <summary>
    /// Stores data which is used to modify the appearance of station AI.
    /// </summary>
    [DataField]
    public Dictionary<string, PrototypeLayerData> LayerData = new();
}
