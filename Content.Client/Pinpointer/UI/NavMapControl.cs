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

                    var tile = (chunk.Origin * SharedNavMapSystem.ChunkSize + SharedNavMapSystem.GetTile(mask)) * grid.TileSize - offset;
                    var position = new Vector2(tile.X, -tile.Y);

                    handle.DrawRect(new UIBox2(Scale(position), Scale(position + tileSize)), Color.Aqua, false);
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
