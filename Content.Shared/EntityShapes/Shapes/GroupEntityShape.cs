namespace Content.Shared.EntityShapes.Shapes;

/// <summary>
/// Picks one shape out of a list of children using weights to randomize between them.
/// </summary>
public sealed partial class GroupEntityShape : EntityShape
{
    [DataField(required: true)]
    public List<EntityShape> Children = new();

    public override TResult Accept<TArgs, TResult>(IEntityShapeVisitor<TArgs, TResult> visitor, TArgs args)
        => visitor.VisitGroupShape(this, args);
}
