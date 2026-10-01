using Content.Shared.Database;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Botany.Components;

/// <summary>
/// Component for storing core plant data.
/// Required as a component to store data specific to the plant type that should not be cloned when the plant's mutable state changes.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class PlantDataComponent : Component
{
    /// <summary>
    /// The noun for this type of seeds. E.g. for fungi this should probably be "spores" instead of "seeds". Also
    /// used to determine the name of seed packets.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId Noun = "seeds-noun-seeds";

    /// <summary>
    /// Name displayed when examining the hydroponics tray. Describes the actual plant, not the seed itself.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public LocId Name;

    /// <summary>
    /// The entity prototype that is spawned when this type of seed is extracted from produce using a seed extractor.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId PacketPrototype;

    /// <summary>
    /// The plant prototypes this plant may mutate into when prompted to.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntProtoId> MutationPrototypes = [];

    /// <summary>
    /// The entity prototypes that are spawned when this type of seed is harvested.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public List<EntProtoId> ProductPrototypes = [];

    /// <summary>
    /// Log impact for harvest operations.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LogImpact? HarvestLogImpact;

    /// <summary>
    /// Log impact for plant operations.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LogImpact? PlantLogImpact;
}
