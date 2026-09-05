using System.Numerics;

namespace Content.Shared.EntityShapes.Shapes;

/// <summary>
/// Represents a simple line with length of Size
/// made in some specified direction.
/// </summary>
public sealed partial class LineEntityShape : EntityShape
{
    [DataField]
    public Vector2 Direction = Vector2.UnitX;

    public override TResult Accept<TArgs, TResult>(IEntityShapeVisitor<TArgs, TResult> visitor, TArgs args)
        => visitor.VisitLineShape(this, args);
}
