using Robust.Shared.Prototypes;

namespace Content.Shared.EntityShapes.Shapes;

/// <summary>
/// Shape that references a ProtoId containing some other shape.
/// </summary>
public sealed partial class NestedEntityShape : EntityShape
{
    public const string IdDataFieldTag = "id";

    [DataField(IdDataFieldTag, required: true)]
    public ProtoId<EntityShapePrototype> Id;

    public override TResult Accept<TArgs, TResult>(IEntityShapeVisitor<TArgs, TResult> visitor, TArgs args)
        => visitor.VisitNestedShape(this, args);
}
