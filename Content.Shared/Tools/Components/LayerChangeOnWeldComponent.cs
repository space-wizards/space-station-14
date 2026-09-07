using Content.Shared.Physics;
using Content.Shared.Tools.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Tools.Components;

/// <summary>
/// This component changes the collision layer of airlocks when they are welded, so rats can't crawl under them
/// </summary>
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
