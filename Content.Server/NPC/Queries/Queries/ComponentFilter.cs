using Robust.Shared.Prototypes;

namespace Content.Server.NPC.Queries.Queries;

public sealed class ComponentFilter : UtilityQueryFilter
{
    /// <summary>
    /// Components to filter for.
    /// </summary>
    [DataField("components", required: true)]
    public ComponentRegistry Components = new();
}
