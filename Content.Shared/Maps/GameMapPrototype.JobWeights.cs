using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared.Maps;

public sealed partial class GameMapPrototype
{
    /// <summary>
    /// Optional map-specific job weighting profile. Jobs omitted from it use the global default profile.
    /// </summary>
    [DataField]
    public ProtoId<JobWeightPrototype>? JobWeights;
}
