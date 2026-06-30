using Robust.Shared.Prototypes;

namespace Content.Shared.EntityTable.EntitySelectors;

/// <summary>
/// A table which simply delegates to the table identified by <see cref="TableId"/>.
/// Can be used to reuse common tables.
/// </summary>
public sealed partial class NestedSelector : EntityTableSelector
{
    [DataField(required: true)]
    public ProtoId<EntityTablePrototype> TableId;

    public override TResult Accept<TContext, TResult>(IEntityTableVisitor<TContext, TResult> visitor, TContext args) =>
        visitor.VisitNestedSelector(this, args);
}
