using Content.Server.Tools.Systems;
using Content.Shared.Physics;

namespace Content.Server.Tools.Components;

[RegisterComponent]
[Access(typeof(WeldableSystem))]
public sealed class LayerChangeOnWeldComponent : Component
{
    [DataField("unWeldedLayer")]
    [ViewVariables(VVAccess.ReadWrite)]
    public CollisionGroup UnWeldedLayer = CollisionGroup.AirlockLayer;

    [DataField("weldedLayer")]
    [ViewVariables(VVAccess.ReadWrite)]
    public CollisionGroup WeldedLayer = CollisionGroup.WallLayer;
}
