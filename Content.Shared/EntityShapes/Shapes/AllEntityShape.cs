using System.Numerics;

namespace Content.Shared.EntityShapes.Shapes;

/// <summary>
/// Tile shape that is made of multiple other shapes.
/// </summary>
public sealed partial class AllEntityShape : EntityShape
{
    [DataField(required: true)]
    public List<EntityShape> Children = new();

    [DataField]
    public Dictionary<string, GroupEntityShapeOptions>? Options;

    public override TResult Accept<TArgs, TResult>(IEntityShapeVisitor<TArgs, TResult> visitor, TArgs args)
        => visitor.VisitAllShape(this, args);
}

[DataDefinition]
public partial record struct GroupEntityShapeOptions
{
    [DataField]
    public Vector2? Offset;

    [DataField]
    public int? GroupSize;

    [DataField]
    public int? GroupStepSize;
}
