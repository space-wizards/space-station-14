using System.Numerics;
using Content.Client.UserInterface.Controls;
using Content.Shared.Pinpointer;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Vector2 = Robust.Shared.Maths.Vector2;

namespace Content.Client.Pinpointer.UI;

/// <summary>
/// Displays the nav map data of the specified grid.
/// </summary>
public sealed class NavMapControl : MapGridControl
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public EntityUid? Uid;

    public NavMapControl() : base(4f, 64f, 32f)
    {
        IoCManager.InjectDependencies(this);
        RectClipContent = true;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        if (!_entManager.TryGetComponent<NavMapComponent>(Uid, out var navMap) ||
            !_entManager.TryGetComponent<TransformComponent>(Uid, out var xform) ||
            !_entManager.TryGetComponent<MapGridComponent>(Uid, out var grid))
        {
            return;
        }

        var offset = Vector2.Zero;

        if (_entManager.TryGetComponent<PhysicsComponent>(Uid, out var physics))
        {
            offset = physics.LocalCenter;
        }

        var area = new Box2(-WorldRange, -WorldRange, WorldRange, WorldRange).Translated(offset);
        var tileSize = new Vector2(grid.TileSize, -grid.TileSize);
        Span<Vector2> verts = stackalloc Vector2[4];

        for (var x = Math.Floor(area.Left); x <= Math.Ceiling(area.Right + 1); x += SharedNavMapSystem.ChunkSize * grid.TileSize)
        {
            for (var y = Math.Floor(area.Bottom); y <= Math.Ceiling(area.Top + 1); y += SharedNavMapSystem.ChunkSize * grid.TileSize)
            {
                var floored = new Vector2i((int) x, (int) y);

                var chunkOrigin = SharedMapSystem.GetChunkIndices(floored, SharedNavMapSystem.ChunkSize);

                if (!navMap.Chunks.TryGetValue(chunkOrigin, out var chunk))
                    continue;

                // TODO: Okay maybe I should just use ushorts lmao...
                for (var i = 0; i < SharedNavMapSystem.ChunkSize * SharedNavMapSystem.ChunkSize; i++)
                {
                    var value = (int) Math.Pow(2, i);

                    var mask = chunk.TileData & value;

                    if (mask == 0x0)
                        continue;

                    // Alright now we'll work out our edges
                    var relativeTile = SharedNavMapSystem.GetTile(mask);
                    var tile = (chunk.Origin * SharedNavMapSystem.ChunkSize + relativeTile) * grid.TileSize - offset;
                    var position = new Vector2(tile.X, -tile.Y);
                    NavMapChunk? neighborChunk;
                    bool neighbor;

                    // North edge
                    if (relativeTile.Y == SharedNavMapSystem.ChunkSize - 1)
                    {
                        neighbor = navMap.Chunks.TryGetValue(chunkOrigin + new Vector2i(0, 1), out neighborChunk) &&
                                      (neighborChunk.TileData &
                                       SharedNavMapSystem.GetFlag(new Vector2i(relativeTile.X, 0))) != 0x0;
                    }
                    else
                    {
                        var flag = SharedNavMapSystem.GetFlag(relativeTile + new Vector2i(0, 1));
                        neighbor = (chunk.TileData & flag) != 0x0;
                    }

                    if (!neighbor)
                    {
                        handle.DrawLine(Scale(position + new Vector2(0f, -grid.TileSize)), Scale(position + tileSize), Color.Aqua);
                    }

                    // East edge
                    if (relativeTile.X == SharedNavMapSystem.ChunkSize - 1)
                    {
                        neighbor = navMap.Chunks.TryGetValue(chunkOrigin + new Vector2i(1, 0), out neighborChunk) &&
                                   (neighborChunk.TileData &
                                    SharedNavMapSystem.GetFlag(new Vector2i(0, relativeTile.Y))) != 0x0;
                    }
                    else
                    {
                        var flag = SharedNavMapSystem.GetFlag(relativeTile + new Vector2i(1, 0));
                        neighbor = (chunk.TileData & flag) != 0x0;
                    }

                    if (!neighbor)
                    {
                        handle.DrawLine(Scale(position + tileSize), Scale(position + new Vector2(grid.TileSize, 0f)), Color.Aqua);
                    }

                    // South edge
                    if (relativeTile.Y == 0)
                    {
                        neighbor = navMap.Chunks.TryGetValue(chunkOrigin + new Vector2i(0, -1), out neighborChunk) &&
                                   (neighborChunk.TileData &
                                    SharedNavMapSystem.GetFlag(new Vector2i(relativeTile.X, SharedNavMapSystem.ChunkSize - 1))) != 0x0;
                    }
                    else
                    {
                        var flag = SharedNavMapSystem.GetFlag(relativeTile + new Vector2i(0, -1));
                        neighbor = (chunk.TileData & flag) != 0x0;
                    }

                    if (!neighbor)
                    {
                        handle.DrawLine(Scale(position + new Vector2(grid.TileSize, 0f)), Scale(position), Color.Aqua);
                    }

                    // West edge
                    if (relativeTile.X == 0)
                    {
                        neighbor = navMap.Chunks.TryGetValue(chunkOrigin + new Vector2i(-1, 0), out neighborChunk) &&
                                   (neighborChunk.TileData &
                                    SharedNavMapSystem.GetFlag(new Vector2i(SharedNavMapSystem.ChunkSize - 1, relativeTile.Y))) != 0x0;
                    }
                    else
                    {
                        var flag = SharedNavMapSystem.GetFlag(relativeTile + new Vector2i(-1, 0));
                        neighbor = (chunk.TileData & flag) != 0x0;
                    }

                    if (!neighbor)
                    {
                        handle.DrawLine(Scale(position), Scale(position + new Vector2(0f, -grid.TileSize)), Color.Aqua);
                    }


                    // handle.DrawRect(new UIBox2(Scale(position), Scale(position + tileSize)), Color.Aqua, false);
                }
            }
        }

        // TODO: Hacky bullshit
        var player = IoCManager.Resolve<IPlayerManager>().LocalPlayer?.ControlledEntity;
        var curTime = Timing.RealTime;
        var blinkFrequency = 1f / 1f;
        var lit = curTime.TotalSeconds % blinkFrequency > blinkFrequency / 2f;

        if (lit && _entManager.TryGetComponent<TransformComponent>(player, out var playerXform))
        {
            var position = xform.InvWorldMatrix.Transform(playerXform.WorldPosition) - offset;
            position = Scale(new Vector2(position.X, -position.Y));

            handle.DrawCircle(position, MinimapScale / 2f, Color.Red);
        }
    }

    private Vector2 Scale(Vector2 position)
    {
        return position * MinimapScale + MidPoint;
    }
}
