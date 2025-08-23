using Robust.Shared.Prototypes;

namespace Content.Server.NPC.Queries.Queries;

/// <summary>
/// This query filter will filter out entities that have any of the components.
/// For the complementary "filter for entities that have any of the components" see <see cref="ComponentFilter"/>
/// </summary>
public sealed partial class RemoveHasComponentFilter : UtilityQueryFilter
{
    /// <summary>
    /// Entities with any of these Components will be filtered out.
    /// Cannot have any of these Components to stay in query. If it has, it will be removed.
    /// </summary>
    [DataField("components", required: true)]
    public ComponentRegistry Components = new();
}
