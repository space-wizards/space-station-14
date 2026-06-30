namespace Content.Shared.EntityTable.EntitySelectors;

/// <summary>
/// Gets the spawns from one of <see cref="Children"/>, based on their <see cref="EntityTableSelector.Weight"/>s.
/// </summary>
public sealed partial class GroupSelector : EntityTableSelector
{
    [DataField(required: true)]
    public List<EntityTableSelector> Children = new();

    public override TResult Accept<TContext, TResult>(IEntityTableVisitor<TContext, TResult> visitor, TContext args) =>
        visitor.VisitGroupSelector(this, args);
}
