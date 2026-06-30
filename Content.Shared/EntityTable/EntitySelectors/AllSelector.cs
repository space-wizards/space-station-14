namespace Content.Shared.EntityTable.EntitySelectors;

/// <summary>
/// Gets spawns from all of <see cref="Children"/>.
/// </summary>
public sealed partial class AllSelector : EntityTableSelector
{
    [DataField(required: true)]
    public List<EntityTableSelector> Children;

    public override TResult Accept<TContext, TResult>(IEntityTableVisitor<TContext, TResult> visitor, TContext args) =>
        visitor.VisitAllSelector(this, args);
}
