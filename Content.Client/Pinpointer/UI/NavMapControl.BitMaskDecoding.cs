using Content.Client.Power;
using Content.Shared.Pinpointer;
using Content.Shared.Power;
using Robust.Shared.Map.Components;
using System.Linq;
using System.Numerics;

namespace Content.Client.Pinpointer.UI;

public sealed partial class NavMapControl
{
    private Dictionary<CableType, Color> _powerCableColors = new Dictionary<CableType, Color>
    {
        [CableType.HighVoltage] = Color.OrangeRed,
        [CableType.MediumVoltage] = Color.Yellow,
        [CableType.Apc] = Color.LimeGreen,
    };

    private Dictionary<CableType, Vector2> _powerCableOffsets = new Dictionary<CableType, Vector2>
    {
        [CableType.HighVoltage] = new Vector2(-0.2f, -0.2f),
        [CableType.MediumVoltage] = Vector2.Zero,
        [CableType.Apc] = new Vector2(0.2f, 0.2f),
    };

    public Dictionary<Vector2i, List<NavMapLine>> GetDecodedPowerCableChunks
        (Dictionary<Vector2i, PowerCableChunk> chunks,
        MapGridComponent grid,
        bool useDarkColors = false)
    {
        var decodedOutput = new Dictionary<Vector2i, List<NavMapLine>>();

        foreach ((var chunkOrigin, var chunk) in chunks)
        {
            var list = new List<NavMapLine>();

            foreach ((var cableType, var chunkMask) in chunk.PowerCableData)
            {
                Vector2 offset = _powerCableOffsets.TryGetValue(cableType, out offset) ? offset : Vector2.Zero;
                Color color = _powerCableColors.TryGetValue(cableType, out color) ? color : Color.White;

                color *= useDarkColors ? Color.DimGray : Color.White;

                for (var i = 0; i < SharedNavMapSystem.ChunkSize * SharedNavMapSystem.ChunkSize; i++)
                {
                    var value = (int) Math.Pow(2, i);
                    var mask = chunkMask & value;

                    if (mask == 0x0)
                        continue;

                    var relativeTile = SharedNavMapSystem.GetTile(mask);
                    var tile = (chunk.Origin * SharedNavMapSystem.ChunkSize + relativeTile) * grid.TileSize;
                    var position = new Vector2(tile.X, -tile.Y);

                    PowerCableChunk? neighborChunk;
                    bool neighbor;

                    // Note: we only check the north and east neighbors

                    // East
                    if (relativeTile.X == SharedNavMapSystem.ChunkSize - 1)
                    {
                        neighbor = chunks.TryGetValue(chunkOrigin + new Vector2i(1, 0), out neighborChunk) &&
                                    neighborChunk.PowerCableData.TryGetValue(cableType, out var neighborChunkMask) &&
                                    (neighborChunkMask & SharedNavMapSystem.GetFlag(new Vector2i(0, relativeTile.Y))) != 0x0;
                    }
                    else
                    {
                        var flag = SharedNavMapSystem.GetFlag(relativeTile + new Vector2i(1, 0));
                        neighbor = (chunkMask & flag) != 0x0;
                    }

                    if (neighbor)
                    {
                        // Add points
                        var line = new NavMapLine
                            (position + offset + new Vector2(grid.TileSize * 0.5f, -grid.TileSize * 0.5f),
                            position + new Vector2(1f, 0f) + offset + new Vector2(grid.TileSize * 0.5f, -grid.TileSize * 0.5f),
                            color,
                            (NavMapLineGroup) cableType);

                        list.Add(line);
                    }

                    // North
                    if (relativeTile.Y == SharedNavMapSystem.ChunkSize - 1)
                    {
                        neighbor = chunks.TryGetValue(chunkOrigin + new Vector2i(0, 1), out neighborChunk) &&
                                    neighborChunk.PowerCableData.TryGetValue(cableType, out var neighborChunkMask) &&
                                    (neighborChunkMask & SharedNavMapSystem.GetFlag(new Vector2i(relativeTile.X, 0))) != 0x0;
                    }
                    else
                    {
                        var flag = SharedNavMapSystem.GetFlag(relativeTile + new Vector2i(0, 1));
                        neighbor = (chunkMask & flag) != 0x0;
                    }

                    if (neighbor)
                    {
                        // Add points
                        var line = new NavMapLine
                            (position + offset + new Vector2(grid.TileSize * 0.5f, -grid.TileSize * 0.5f),
                            position + new Vector2(0f, -1f) + offset + new Vector2(grid.TileSize * 0.5f, -grid.TileSize * 0.5f),
                            color,
                            (NavMapLineGroup) cableType);

                        list.Add(line);
                    }
                }

            }

            if (list.Any())
                decodedOutput.Add(chunkOrigin, list);
        }

        return decodedOutput;
    }

    public Dictionary<Vector2i, List<NavMapLine>> GetDecodedTileChunks
        (Dictionary<Vector2i, NavMapChunk> chunks,
        MapGridComponent grid)
    {
        var decodedOutput = new Dictionary<Vector2i, List<NavMapLine>>();

        foreach ((var chunkOrigin, var chunk) in chunks)
        {
            var list = new List<NavMapLine>();

            // TODO: Okay maybe I should just use ushorts lmao...
            for (var i = 0; i < SharedNavMapSystem.ChunkSize * SharedNavMapSystem.ChunkSize; i++)
            {
                var value = (int) Math.Pow(2, i);

                var mask = chunk.TileData & value;

                if (mask == 0x0)
                    continue;

                // Alright now we'll work out our edges
                var relativeTile = SharedNavMapSystem.GetTile(mask);
                var tile = (chunk.Origin * SharedNavMapSystem.ChunkSize + relativeTile) * grid.TileSize;
                var position = new Vector2(tile.X, -tile.Y);
                NavMapChunk? neighborChunk;
                bool neighbor;

                // North edge
                if (relativeTile.Y == SharedNavMapSystem.ChunkSize - 1)
                {
                    neighbor = chunks.TryGetValue(chunkOrigin + new Vector2i(0, 1), out neighborChunk) &&
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
                    // Add points
                    list.Add(new NavMapLine(position + new Vector2(0f, -grid.TileSize), position + new Vector2(grid.TileSize, -grid.TileSize), WallColor));
                }

                // East edge
                if (relativeTile.X == SharedNavMapSystem.ChunkSize - 1)
                {
                    neighbor = chunks.TryGetValue(chunkOrigin + new Vector2i(1, 0), out neighborChunk) &&
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
                    // Add points
                    list.Add(new NavMapLine(position + new Vector2(grid.TileSize, -grid.TileSize), position + new Vector2(grid.TileSize, 0f), WallColor));
                }

                // South edge
                if (relativeTile.Y == 0)
                {
                    neighbor = chunks.TryGetValue(chunkOrigin + new Vector2i(0, -1), out neighborChunk) &&
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
                    // Add points
                    list.Add(new NavMapLine(position + new Vector2(grid.TileSize, 0f), position, WallColor));
                }

                // West edge
                if (relativeTile.X == 0)
                {
                    neighbor = chunks.TryGetValue(chunkOrigin + new Vector2i(-1, 0), out neighborChunk) &&
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
                    // Add point
                    list.Add(new NavMapLine(position, position + new Vector2(0f, -grid.TileSize), WallColor));
                }

                // Draw a diagonal line for interiors.
                list.Add(new NavMapLine(position + new Vector2(0f, -grid.TileSize), position + new Vector2(grid.TileSize, 0f), WallColor));
            }

            decodedOutput.Add(chunkOrigin, list);
        }

        return decodedOutput;
    }
}

public struct NavMapLine
{
    public readonly Vector2 Origin;
    public readonly Vector2 Terminus;
    public readonly Color Color;
    public readonly NavMapLineGroup Group;

    public NavMapLine(Vector2 origin, Vector2 terminus, Color color, NavMapLineGroup group = NavMapLineGroup.Wall)
    {
        Origin = origin;
        Terminus = terminus;
        Color = color;
        Group = group;
    }
}

public enum NavMapLineGroup : byte
{
    HighVoltage,
    MediumVoltage,
    Apc,
    Wall,
}
