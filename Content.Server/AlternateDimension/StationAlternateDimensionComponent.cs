using Content.Shared.AlternateDimension;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.AlternateDimension;

/// <summary>
/// When the station is initialized, generates an alternate reality according to specified parameters.
/// </summary>
[RegisterComponent]
public sealed partial class StationAlternateDimensionComponent : Component
{
    /// <summary>
    /// Reference to the mapId of the created alternate reality.
    /// </summary>
    [DataField]
    public MapId? DimensionId;

    [DataField(required: true)]
    public ProtoId<AlternateDimensionPrototype> Dimension = string.Empty;
}
