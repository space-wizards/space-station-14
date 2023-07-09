using System.Numerics;
using Content.Shared.IconSmoothing;
using Robust.Client.GameObjects;

namespace Content.Client.IconSmoothing;

public sealed partial class IconSmoothSystem
{
    // Handles drawing edge sprites on the non-smoothed edges.

    private void InitializeEdge()
    {
        SubscribeLocalEvent<SmoothEdgeComponent, ComponentStartup>(OnEdgeStartup);
        SubscribeLocalEvent<SmoothEdgeComponent, ComponentShutdown>(OnEdgeShutdown);
    }

    private void OnEdgeStartup(EntityUid uid, SmoothEdgeComponent component, ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        sprite.LayerSetOffset(EdgeLayer.South, new Vector2(0, -1f));
        sprite.LayerSetOffset(EdgeLayer.East, new Vector2(1f, 0f));
        sprite.LayerSetOffset(EdgeLayer.North, new Vector2(0, 1f));
        sprite.LayerSetOffset(EdgeLayer.West, new Vector2(-1f, 0f));

        sprite.LayerSetVisible(EdgeLayer.South, false);
        sprite.LayerSetVisible(EdgeLayer.East, false);
        sprite.LayerSetVisible(EdgeLayer.North, false);
        sprite.LayerSetVisible(EdgeLayer.West, false);
    }

    private void OnEdgeShutdown(EntityUid uid, SmoothEdgeComponent component, ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        sprite.LayerMapRemove(EdgeLayer.South);
        sprite.LayerMapRemove(EdgeLayer.East);
        sprite.LayerMapRemove(EdgeLayer.North);
        sprite.LayerMapRemove(EdgeLayer.West);
    }

    private void CalculateEdge(EntityUid uid, DirectionFlag directions, SpriteComponent? sprite = null, SmoothEdgeComponent? component = null)
    {
        if (!Resolve(uid, ref sprite, ref component, false))
            return;

        for (var i = 0; i < 4; i++)
        {
            var dir = (DirectionFlag) Math.Pow(2, i);
            var edge = GetEdge(dir);

            if ((dir & directions) != 0x0)
            {
                sprite.LayerSetVisible(edge, false);
                continue;
            }

            sprite.LayerSetVisible(edge, true);
        }
    }

    private EdgeLayer GetEdge(DirectionFlag direction)
    {
        switch (direction)
        {
            case DirectionFlag.South:
                return EdgeLayer.South;
            case DirectionFlag.East:
                return EdgeLayer.East;
            case DirectionFlag.North:
                return EdgeLayer.North;
            case DirectionFlag.West:
                return EdgeLayer.West;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private enum EdgeLayer : byte
    {
        South,
        East,
        North,
        West
    }
}
