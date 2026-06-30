using Content.Shared.EntityTable.ValueSelector;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityTable.EntitySelectors;

/// <summary>
/// A table which selects <see cref="Id"/>, <see cref="EntityTableSelector.Rolls"/> times.
/// </summary>
public sealed partial class EntSelector : EntityTableSelector
{
    public const string IdDataFieldTag = "id";

    // The const string is used in a specialized serializer.
#pragma warning disable RA0027
    [DataField(IdDataFieldTag, required: true)]
#pragma warning restore RA0027
    public EntProtoId Id;

    [DataField]
    public NumberSelector Amount = new ConstantNumberSelector(1);

    public override TResult Accept<TContext, TResult>(IEntityTableVisitor<TContext, TResult> visitor, TContext args) =>
        visitor.VisitEntSelector(this, args);
}
