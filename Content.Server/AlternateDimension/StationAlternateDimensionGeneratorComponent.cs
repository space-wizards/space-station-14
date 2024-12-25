using Content.Shared.AlternateDimension;
using Robust.Shared.Prototypes;

namespace Content.Server.AlternateDimension;

/// <summary>
/// When the station is initialized, generates an alternate reality according to specified parameters.
/// </summary>
[RegisterComponent]
public sealed partial class StationAlternateDimensionGeneratorComponent : Component
{
    [DataField(required: true)]
    public ProtoId<AlternateDimensionPrototype> Dimension = string.Empty;
}
