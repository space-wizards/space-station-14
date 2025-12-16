using Content.Shared.Database;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Botany.Components;

/// <summary>
/// Component for storing core plant data.
/// </summary>
[RegisterComponent]
[DataDefinition]
public sealed partial class PlantDataComponent : Component
{
    /// <summary>
    /// The noun for this type of seeds. E.g. for fungi this should probably be "spores" instead of "seeds". Also
    /// used to determine the name of seed packets.
    /// </summary>
    [DataField]
    public string Noun { get; private set; } = "";

    /// <summary>
    /// Name displayed when examining the hydroponics tray. Describes the actual plant, not the seed itself.
    /// </summary>
    [DataField]
    public string DisplayName { get; private set; } = "";

    /// <summary>
    /// The entity prototype that is spawned when this type of seed is extracted from produce using a seed extractor.
    /// </summary>
    [DataField]
    public EntProtoId PacketPrototype = "SeedBase";

    /// <summary>
    /// The plant prototypes this plant may mutate into when prompted to.
    /// </summary>
    [DataField]
    public List<EntProtoId> MutationPrototypes = [];

    /// <summary>
    /// The entity prototypes that are spawned when this type of seed is harvested.
    /// </summary>
    [DataField]
    public List<EntProtoId> ProductPrototypes = [];

    /// <summary>
    /// The RSI path to the plant's sprite.
    /// </summary>
    [DataField(required: true)]
    public ResPath PlantRsi { get; set; } = default!;

    /// <summary>
    /// Log impact for harvest operations.
    /// </summary>
    [DataField]
    public LogImpact? HarvestLogImpact;

    /// <summary>
    /// Log impact for plant operations.
    /// </summary>
    [DataField]
    public LogImpact? PlantLogImpact;
}
