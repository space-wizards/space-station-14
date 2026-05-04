
using System.Numerics;

namespace Content.Client.DirectionalArrowIndicator;

[RegisterComponent]
public sealed partial class DirectionalArrowIndicatorComponent : Component
{
    [DataField]
    public float Lifetime = 2f;

    [DataField]
    public List<ArrowSpawnData> Arrows = new();
}

[DataDefinition]
public sealed partial class ArrowSpawnData
{
    [DataField]
    public Vector2 Offset = Vector2.Zero;

    [DataField]
    public Angle Rotation = Angle.Zero;
}
