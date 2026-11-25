using Content.Shared.Physics;
using Content.Shared.Tools.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Tools.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(WeldableSystem))]
public sealed partial class LayerChangeOnWeldComponent : Component
{
    [DataField("unWeldedLayer")]
    [ViewVariables]
    public CollisionGroup UnWeldedLayer = CollisionGroup.AirlockLayer;

    [DataField("weldedLayer")]
    [ViewVariables]
    public CollisionGroup WeldedLayer = CollisionGroup.WallLayer;
}
