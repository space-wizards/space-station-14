using Robust.Shared.Prototypes;

namespace Content.Server.NPC.Queries.Queries;

/// <summary>
/// This query filter will filter for entities that have any of the components.
/// For the complementary "filter out entities that have any of the components" see <see cref="RemoveHasComponentFilter"/>
/// </summary>
public sealed partial class ComponentFilter : UtilityQueryFilter
{
    /// <summary>
    /// Entities with any of these Components will be filtered for.
    /// Must have any of these Components to stay in query. If it has not, it will be removed.
    /// </summary>
    [DataField("components", required: true)]
    public ComponentRegistry Components = new();
}
