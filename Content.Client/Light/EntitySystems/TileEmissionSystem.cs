using System.Numerics;
using Content.Client.Light.Components;
using Content.Shared.Light.Components;
using Robust.Shared.ComponentTrees;
using Robust.Shared.Physics;

namespace Content.Client.Light.EntitySystems;

public sealed class TileEmissionSystem : ComponentTreeSystem<TileEmissionTreeComponent, TileEmissionComponent>
{
    protected override bool DoFrameUpdate => true;
    protected override bool DoTickUpdate => true;
    protected override bool Recursive => false;
    protected override Box2 ExtractAabb(in ComponentTreeEntry<TileEmissionComponent> entry, Vector2 pos, Angle rot)
    {
        var boxR = new Box2Rotated(Box2.CenteredAround(pos, new Vector2(entry.Component.Range)), rot);
        return boxR.CalcBoundingBox();
    }
}
