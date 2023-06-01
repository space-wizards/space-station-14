using Robust.Shared.Prototypes;

namespace Content.Server.NPC.Queries.Queries;

public sealed class NearbyComponentsQuery : UtilityQuery
{
    [DataField("components")]
    public ComponentRegistry Component = default!;
}
