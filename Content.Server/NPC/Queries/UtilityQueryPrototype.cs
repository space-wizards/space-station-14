using Robust.Shared.Prototypes;

namespace Content.Server.NPC.Queries;

/// <summary>
/// Stores data for generic queries.
/// Each query is run in turn to get the final available results.
/// These results are then run through the considerations.
/// </summary>
[Prototype("utilityQuery")]
public sealed class UtilityQueryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [ViewVariables(VVAccess.ReadWrite), DataField("query")]
    public List<UtilityQuery> Query = new();

    [ViewVariables(VVAccess.ReadWrite), DataField("considerations")]
    public List<UtilityConsideration> Considerations = new();
}

public sealed class NearbyHostilesQuery : UtilityQuery
{

}

public abstract class UtilityConsideration
{

}


/// <summary>
/// Removes entities from a query.
/// </summary>
public abstract class UtilityQueryFilter : UtilityQuery
{

}

/// <summary>
/// Adds entities to a query.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract class UtilityQuery
{

}
