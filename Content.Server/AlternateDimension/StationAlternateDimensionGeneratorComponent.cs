using Content.Shared.AlternateDimension;
using Robust.Shared.Prototypes;

namespace Content.Server.AlternateDimension;

/// <summary>
/// When the station is initialized, generates one of the random alternative measurements specified in the list for the largest station grid
/// </summary>
[RegisterComponent]
public sealed partial class StationAlternateDimensionGeneratorComponent : Component
{
    [DataField(required: true)]
    public List<ProtoId<AlternateDimensionPrototype>> Dimensions = new();
}
