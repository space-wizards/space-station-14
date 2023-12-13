using Content.Client.Pinpointer.UI;
using Content.Shared.Pinpointer;
using Content.Shared.Power;
using Robust.Client.Graphics;
using Robust.Shared.Collections;
using Robust.Shared.Map.Components;
using System.Numerics;

namespace Content.Client.Power;

public sealed partial class PowerMonitoringConsoleNavMapControl : NavMapControl
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    // Cable indexing
    // 0: CableType.HighVoltage
    // 1: CableType.MediumVoltage
    // 2: CableType.Apc

    private readonly Color[] _powerCableColors = { Color.OrangeRed, Color.Yellow, Color.LimeGreen };
    private readonly Vector2[] _powerCableOffsets = { new Vector2(-0.2f, -0.2f), Vector2.Zero, new Vector2(0.2f, 0.2f) };
    private Dictionary<Color, Color> _sRGBLookUp = new Dictionary<Color, Color>();

    public PowerMonitoringCableNetworksComponent? PowerMonitoringCableNetworks;
    public List<PowerMonitoringConsoleLineGroup> HiddenLineGroups = new();
    public Dictionary<Vector2i, List<PowerMonitoringConsoleLine>>? PowerCableNetwork;
    public Dictionary<Vector2i, List<PowerMonitoringConsoleLine>>? FocusCableNetwork;

    private MapGridComponent? _grid;

    public PowerMonitoringConsoleNavMapControl() : base()
    {
        // Set colors
        TileColor = new Color(30, 57, 67);
        WallColor = new Color(102, 164, 217);

        PostWallDrawingAction += DrawAllCableNetworks;
    }

    protected override void UpdateNavMap()
    {
        base.UpdateNavMap();

        if (Owner == null)
            return;

        if (!_entManager.TryGetComponent<PowerMonitoringCableNetworksComponent>(Owner, out var cableNetworks))
            return;

        if (!_entManager.TryGetComponent(MapUid, out _grid))
            return;

        PowerCableNetwork = GetDecodedPowerCableChunks(cableNetworks.AllChunks, _grid);
        FocusCableNetwork = GetDecodedPowerCableChunks(cableNetworks.FocusChunks, _grid);
    }

    public void DrawAllCableNetworks(DrawingHandleScreen handle)
    {
        // Draw full cable network
        if (PowerCableNetwork != null && PowerCableNetwork.Count > 0)
        {
            var modulator = (FocusCableNetwork != null && FocusCableNetwork.Count > 0) ? Color.DimGray : Color.White;
            DrawCableNetwork(handle, PowerCableNetwork, modulator);
        }

        // Draw focus network
        if (FocusCableNetwork != null && FocusCableNetwork.Count > 0)
            DrawCableNetwork(handle, FocusCableNetwork, Color.White);
    }

    public void DrawCableNetwork(DrawingHandleScreen handle, Dictionary<Vector2i, List<PowerMonitoringConsoleLine>> fullCableNetwork, Color modulator)
    {
        var offset = GetOffset();
        var area = new Box2(-WorldRange, -WorldRange, WorldRange + 1f, WorldRange + 1f).Translated(offset);

        if (WorldRange / WorldMaxRange > 0.5f)
        {
            var cableNetworks = new ValueList<Vector2>[3];

            foreach ((var chunk, var chunkedLines) in fullCableNetwork)
            {
                var offsetChunk = new Vector2(chunk.X, chunk.Y) * SharedNavMapSystem.ChunkSize;

                if (offsetChunk.X < area.Left - SharedNavMapSystem.ChunkSize || offsetChunk.X > area.Right)
                    continue;

                if (offsetChunk.Y < area.Bottom - SharedNavMapSystem.ChunkSize || offsetChunk.Y > area.Top)
                    continue;

                foreach (var chunkedLine in chunkedLines)
                {
                    if (HiddenLineGroups.Contains(chunkedLine.Group))
                        continue;

                    var start = Scale(chunkedLine.Origin - new Vector2(offset.X, -offset.Y));
                    var end = Scale(chunkedLine.Terminus - new Vector2(offset.X, -offset.Y));

                    cableNetworks[(int) chunkedLine.Group].Add(start);
                    cableNetworks[(int) chunkedLine.Group].Add(end);
                }
            }

            for (int cableNetworkIdx = 0; cableNetworkIdx < cableNetworks.Length; cableNetworkIdx++)
            {
                var cableNetwork = cableNetworks[cableNetworkIdx];

                if (cableNetwork.Count > 0)
                {
                    var color = _powerCableColors[cableNetworkIdx] * modulator;

                    if (!_sRGBLookUp.TryGetValue(color, out var sRGB))
                    {
                        sRGB = Color.ToSrgb(color);
                        _sRGBLookUp[color] = sRGB;
                    }

                    handle.DrawPrimitives(DrawPrimitiveTopology.LineList, cableNetwork.Span, sRGB);
                }
            }
        }

        else
        {
            var cableVertexUVs = new ValueList<Vector2>[3];

            foreach ((var chunk, var chunkedLines) in fullCableNetwork)
            {
                var offsetChunk = new Vector2(chunk.X, chunk.Y) * SharedNavMapSystem.ChunkSize;

                if (offsetChunk.X < area.Left - SharedNavMapSystem.ChunkSize || offsetChunk.X > area.Right)
                    continue;

                if (offsetChunk.Y < area.Bottom - SharedNavMapSystem.ChunkSize || offsetChunk.Y > area.Top)
                    continue;

                foreach (var chunkedLine in chunkedLines)
                {
                    if (HiddenLineGroups.Contains(chunkedLine.Group))
                        continue;

                    var leftTop = Scale(new Vector2
                        (Math.Min(chunkedLine.Origin.X, chunkedLine.Terminus.X) - 0.1f,
                        Math.Min(chunkedLine.Origin.Y, chunkedLine.Terminus.Y) - 0.1f)
                        - new Vector2(offset.X, -offset.Y));

                    var rightTop = Scale(new Vector2
                        (Math.Max(chunkedLine.Origin.X, chunkedLine.Terminus.X) + 0.1f,
                        Math.Min(chunkedLine.Origin.Y, chunkedLine.Terminus.Y) - 0.1f)
                        - new Vector2(offset.X, -offset.Y));

                    var leftBottom = Scale(new Vector2
                        (Math.Min(chunkedLine.Origin.X, chunkedLine.Terminus.X) - 0.1f,
                        Math.Max(chunkedLine.Origin.Y, chunkedLine.Terminus.Y) + 0.1f)
                        - new Vector2(offset.X, -offset.Y));

                    var rightBottom = Scale(new Vector2
                        (Math.Max(chunkedLine.Origin.X, chunkedLine.Terminus.X) + 0.1f,
                        Math.Max(chunkedLine.Origin.Y, chunkedLine.Terminus.Y) + 0.1f)
                        - new Vector2(offset.X, -offset.Y));

                    cableVertexUVs[(int) chunkedLine.Group].Add(leftBottom);
                    cableVertexUVs[(int) chunkedLine.Group].Add(leftTop);
                    cableVertexUVs[(int) chunkedLine.Group].Add(rightBottom);
                    cableVertexUVs[(int) chunkedLine.Group].Add(leftTop);
                    cableVertexUVs[(int) chunkedLine.Group].Add(rightBottom);
                    cableVertexUVs[(int) chunkedLine.Group].Add(rightTop);
                }
            }

            for (int cableNetworkIdx = 0; cableNetworkIdx < cableVertexUVs.Length; cableNetworkIdx++)
            {
                var cableVertexUV = cableVertexUVs[cableNetworkIdx];

                if (cableVertexUV.Count > 0)
                {
                    var color = _powerCableColors[cableNetworkIdx] * modulator;

                    if (!_sRGBLookUp.TryGetValue(color, out var sRGB))
                    {
                        sRGB = Color.ToSrgb(color);
                        _sRGBLookUp[color] = sRGB;
                    }

                    handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, cableVertexUV.Span, sRGB);
                }
            }
        }
    }

    public Dictionary<Vector2i, List<PowerMonitoringConsoleLine>>? GetDecodedPowerCableChunks(Dictionary<Vector2i, PowerCableChunk>? chunks, MapGridComponent? grid)
    {
        if (chunks == null || grid == null)
            return null;

        var decodedOutput = new Dictionary<Vector2i, List<PowerMonitoringConsoleLine>>();

        foreach ((var chunkOrigin, var chunk) in chunks)
        {
            var list = new List<PowerMonitoringConsoleLine>();

            for (int cableIdx = 0; cableIdx < chunk.PowerCableData.Length; cableIdx++)
            {
                var chunkMask = chunk.PowerCableData[cableIdx];

                Vector2 offset = _powerCableOffsets[cableIdx];

                for (var chunkIdx = 0; chunkIdx < SharedNavMapSystem.ChunkSize * SharedNavMapSystem.ChunkSize; chunkIdx++)
                {
                    var value = (int) Math.Pow(2, chunkIdx);
                    var mask = chunkMask & value;

                    if (mask == 0x0)
                        continue;

                    var relativeTile = SharedNavMapSystem.GetTile(mask);
                    var tile = (chunk.Origin * SharedNavMapSystem.ChunkSize + relativeTile) * grid.TileSize;
                    var position = new Vector2(tile.X, -tile.Y);

                    PowerCableChunk neighborChunk;
                    bool neighbor;

                    // Note: we only check the north and east neighbors

                    // East
                    if (relativeTile.X == SharedNavMapSystem.ChunkSize - 1)
                    {
                        neighbor = chunks.TryGetValue(chunkOrigin + new Vector2i(1, 0), out neighborChunk) &&
                                    (neighborChunk.PowerCableData[cableIdx] & SharedNavMapSystem.GetFlag(new Vector2i(0, relativeTile.Y))) != 0x0;
                    }
                    else
                    {
                        var flag = SharedNavMapSystem.GetFlag(relativeTile + new Vector2i(1, 0));
                        neighbor = (chunkMask & flag) != 0x0;
                    }

                    if (neighbor)
                    {
                        // Add points
                        var line = new PowerMonitoringConsoleLine
                            (position + offset + new Vector2(grid.TileSize * 0.5f, -grid.TileSize * 0.5f),
                            position + new Vector2(1f, 0f) + offset + new Vector2(grid.TileSize * 0.5f, -grid.TileSize * 0.5f),
                            (PowerMonitoringConsoleLineGroup) cableIdx);

                        list.Add(line);
                    }

                    // North
                    if (relativeTile.Y == SharedNavMapSystem.ChunkSize - 1)
                    {
                        neighbor = chunks.TryGetValue(chunkOrigin + new Vector2i(0, 1), out neighborChunk) &&
                                    (neighborChunk.PowerCableData[cableIdx] & SharedNavMapSystem.GetFlag(new Vector2i(relativeTile.X, 0))) != 0x0;
                    }
                    else
                    {
                        var flag = SharedNavMapSystem.GetFlag(relativeTile + new Vector2i(0, 1));
                        neighbor = (chunkMask & flag) != 0x0;
                    }

                    if (neighbor)
                    {
                        // Add points
                        var line = new PowerMonitoringConsoleLine
                            (position + offset + new Vector2(grid.TileSize * 0.5f, -grid.TileSize * 0.5f),
                            position + new Vector2(0f, -1f) + offset + new Vector2(grid.TileSize * 0.5f, -grid.TileSize * 0.5f),
                            (PowerMonitoringConsoleLineGroup) cableIdx);

                        list.Add(line);
                    }
                }

            }

            if (list.Count > 0)
                decodedOutput.Add(chunkOrigin, list);
        }

        return decodedOutput;
    }
}

public struct PowerMonitoringConsoleLine
{
    public readonly Vector2 Origin;
    public readonly Vector2 Terminus;
    public readonly PowerMonitoringConsoleLineGroup Group;

    public PowerMonitoringConsoleLine(Vector2 origin, Vector2 terminus, PowerMonitoringConsoleLineGroup group)
    {
        Origin = origin;
        Terminus = terminus;
        Group = group;
    }
}

public enum PowerMonitoringConsoleLineGroup : byte
{
    HighVoltage,
    MediumVoltage,
    Apc,
}
