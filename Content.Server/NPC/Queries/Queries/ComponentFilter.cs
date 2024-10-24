using Robust.Shared.Prototypes;

namespace Content.Server.NPC.Queries.Queries;

public sealed partial class ComponentFilter : UtilityQueryFilter
{
    /// <summary>
    /// Entities must have these components
    /// </summary>
    [DataField("components")]
    public ComponentRegistry Components = new();

    /// <summary>
    /// Remove entities which have this component
    /// </summary>
    [DataField("excludedComponents")]
    public ComponentRegistry ExcludedComponents = new();
}
