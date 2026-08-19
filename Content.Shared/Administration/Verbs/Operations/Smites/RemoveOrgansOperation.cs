using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared.Administration.Verbs.Operations.Smites;

/// <summary>
/// Removes matching organs from a body. By default, removed organs are detached into the world.
/// </summary>
public sealed partial class RemoveOrgansOperation : AdminOperationBase<RemoveOrgansOperation>
{
    /// <summary>
    /// If null, organs from any category may be removed.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<OrganCategoryPrototype>>? Categories { get; private set; }

    /// <summary>
    /// Exclusions take precedence over <see cref="Categories"/>.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<OrganCategoryPrototype>> ExcludedCategories { get; private set; } = [];

    /// <summary>
    /// Deletes selected organs instead of detaching them.
    /// </summary>
    [DataField]
    public bool Delete { get; private set; }

    /// <summary>
    /// Null removes every match. Values less than or equal to zero remove nothing.
    /// </summary>
    [DataField]
    public int? MaxCount { get; private set; }
}
