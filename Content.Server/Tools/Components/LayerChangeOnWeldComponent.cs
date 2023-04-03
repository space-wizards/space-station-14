using Content.Server.Tools.Systems;
using Content.Shared.Physics;

namespace Content.Server.Tools.Components;

[RegisterComponent]
[Access(typeof(WeldableSystem))]
public sealed class LayerChangeOnWeldComponent : Component
{
    [DataField("unWeldedLayer")]
    [ViewVariables]
    public CollisionGroup UnWeldedLayer = CollisionGroup.AirlockLayer;

    [DataField("weldedLayer")]
    [ViewVariables]
    public CollisionGroup WeldedLayer = CollisionGroup.WallLayer;
}
