using Content.Shared.EntityShapes.Shapes;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityShapes;

/// <summary>
/// Contains an EntityShape for common use.
/// </summary>
[Prototype]
public sealed partial class EntityShapePrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public EntityShape Shape = default!;
}
