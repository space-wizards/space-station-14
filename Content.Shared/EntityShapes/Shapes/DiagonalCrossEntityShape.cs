namespace Content.Shared.EntityShapes.Shapes;

/// <summary>
/// Represents a simple shape out of two diagonal lines combined.
/// </summary>
public sealed partial class DiagonalCrossEntityShape : EntityShape
{
    public override TResult Accept<TArgs, TResult>(IEntityShapeVisitor<TArgs, TResult> visitor, TArgs args)
        => visitor.VisitDiagonalCrossShape(this, args);
}
