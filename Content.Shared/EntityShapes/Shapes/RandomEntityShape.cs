namespace Content.Shared.EntityShapes.Shapes;

/// <summary>
/// Picks tiles from another shape as a fixed amount or with some percentage chance.
/// </summary>
public sealed partial class RandomEntityShape : EntityShape
{
    [DataField(required: true)]
    public EntityShape Shape;

    /// <summary>
    /// The chance for a tile to be filled in a shape.
    /// Always overrides <see cref="Amount"/>.
    /// </summary>
    [DataField]
    public float? FilledChance;

    /// <summary>
    /// How many tiles we should randomly include from a shape.
    /// </summary>
    [DataField]
    public int? Amount;

    public override TResult Accept<TArgs, TResult>(IEntityShapeVisitor<TArgs, TResult> visitor, TArgs args)
        => visitor.VisitRandomShape(this, args);
}
