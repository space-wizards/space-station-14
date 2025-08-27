using Robust.Shared.Prototypes;

namespace Content.Shared.Silicons.StationAi;

/// <summary>
/// Holds data for customizing the appearance of station AIs.
/// </summary>
[Prototype]
public sealed partial class StationAiCustomizationGroupPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    /// <summary>
    /// The localized name of the customization.
    /// </summary>
    [DataField(required: true)]
    public LocId Name;

    /// <summary>
    /// The type of customization that is associated with this group.
    /// </summary>
    [DataField]
    public StationAiCustomizationType Category = StationAiCustomizationType.CoreIconography;

    /// <summary>
    /// The list of prototypes associated with the customization group.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<StationAiCustomizationPrototype>> ProtoIds = new();
}
