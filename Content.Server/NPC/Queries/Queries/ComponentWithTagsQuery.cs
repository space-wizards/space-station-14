using Robust.Shared.Prototypes;

namespace Content.Server.NPC.Queries.Queries;

/// <summary>
/// Returns nearby components that match the specified components
/// and match all tags.
/// </summary>
public sealed partial class ComponentWithTagsQuery : UtilityQuery
{
    [DataField("components", required: true)]
    public ComponentRegistry Components = default!;

    [DataField("tags", required: true)]
    public HashSet<string> Tags = default!;
}
