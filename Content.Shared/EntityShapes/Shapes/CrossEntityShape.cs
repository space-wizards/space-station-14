namespace Content.Shared.EntityShapes.Shapes;

/// <summary>
/// Represents a simple shape out of one horizontal and one vertical line combined.
/// </summary>
public sealed partial class CrossEntityShape : EntityShape
{
    public override TResult Accept<TArgs, TResult>(IEntityShapeVisitor<TArgs, TResult> visitor, TArgs args)
        => visitor.VisitCrossShape(this, args);
}
