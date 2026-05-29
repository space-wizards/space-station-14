using System.Numerics;
using Content.Client.Wall.Components;
using Content.Shared.Wall;
using Robust.Client.GameObjects;
using Robust.Shared.ComponentTrees;
using Robust.Shared.Physics;

namespace Content.Client.Wall.Systems;

public sealed partial class WallMountTreeSystem : ComponentTreeSystem<WallMountTreeComponent, WallMountComponent>
{
    [Dependency] private SpriteSystem _sprite = default!;

    protected override bool DoFrameUpdate => true;
    protected override bool DoTickUpdate => false;
    protected override bool Recursive => false;

    protected override Box2 ExtractAabb(in ComponentTreeEntry<WallMountComponent> entry, Vector2 pos, Angle rot)
    {
        return _sprite.CalculateBounds((entry.Uid, Comp<SpriteComponent>(entry.Uid)), pos, rot, default).CalcBoundingBox();
    }
}
