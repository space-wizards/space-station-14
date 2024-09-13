using Content.Client.Pinpointer.UI;
using Content.Shared.Atmos.Components;
using Content.Shared.Pinpointer;
using Robust.Client.Graphics;
using Robust.Shared.Collections;
using Robust.Shared.Map.Components;
using System;
using System.Linq;
using System.Numerics;

namespace Content.Client.Atmos.Console;

public sealed partial class AtmosMonitoringConsoleNavMapControl : NavMapControl
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    private Dictionary<Color, Color> _sRGBLookUp = new Dictionary<Color, Color>();

    public static int ChunkSize = 4;
    public Dictionary<Vector2i, List<AtmosMonitoringConsoleLine>>? AtmosPipeNetwork;
    public bool ShowPipeNetwork = true;

    public AtmosMonitoringConsoleNavMapControl() : base()
    {
        // Set colors
        WallColor = new Color(64, 64, 64);
        TileColor = Color.DimGray * WallColor;

        PostWallDrawingAction += DrawAllPipeNetworks;
    }

    protected override void UpdateNavMap()
    {
        base.UpdateNavMap();

        if (Owner == null)
            return;

        if (!_entManager.TryGetComponent<AtmosMonitoringConsoleComponent>(Owner, out var console))
            return;

        if (!_entManager.TryGetComponent<MapGridComponent>(MapUid, out var grid))
            return;

        AtmosPipeNetwork = GetDecodedAtmosPipeChunks(console.AtmosPipeChunks, grid, console.FocusNetId);
    }

    public void DrawAllPipeNetworks(DrawingHandleScreen handle)
    {
        if (!ShowPipeNetwork)
            return;

        // Draw networks
        if (AtmosPipeNetwork != null && AtmosPipeNetwork.Any())
            DrawPipeNetwork(handle, AtmosPipeNetwork);
    }

    public void DrawPipeNetwork(DrawingHandleScreen handle, Dictionary<Vector2i, List<AtmosMonitoringConsoleLine>> atmosPipeNetwork)
    {
        var offset = GetOffset();
        var area = new Box2(-WorldRange, -WorldRange, WorldRange + 1f, WorldRange + 1f).Translated(offset);

        if (WorldRange / WorldMaxRange > 0.5f)
        {
            var pipeNetworks = new Dictionary<Color, ValueList<Vector2>>();

            foreach ((var chunk, var chunkedLines) in atmosPipeNetwork)
            {
                var offsetChunk = new Vector2(chunk.X, chunk.Y) * ChunkSize;

                if (offsetChunk.X < area.Left - ChunkSize || offsetChunk.X > area.Right)
                    continue;

                if (offsetChunk.Y < area.Bottom - ChunkSize || offsetChunk.Y > area.Top)
                    continue;

                foreach (var chunkedLine in chunkedLines)
                {
                    var start = ScalePosition(chunkedLine.Origin - new Vector2(offset.X, -offset.Y));
                    var end = ScalePosition(chunkedLine.Terminus - new Vector2(offset.X, -offset.Y));

                    if (!pipeNetworks.TryGetValue(chunkedLine.Color, out var subNetwork))
                        subNetwork = new ValueList<Vector2>();

                    subNetwork.Add(start);
                    subNetwork.Add(end);

                    pipeNetworks[chunkedLine.Color] = subNetwork;
                }
            }

            foreach ((var color, var subNetwork) in pipeNetworks)
            {
                if (subNetwork.Count > 0)
                    handle.DrawPrimitives(DrawPrimitiveTopology.LineList, subNetwork.Span, color);
            }
        }

        else
        {
            var pipeVertexUVs = new Dictionary<Color, ValueList<Vector2>>();

            foreach ((var chunk, var chunkedLines) in atmosPipeNetwork)
            {
                var offsetChunk = new Vector2(chunk.X, chunk.Y) * ChunkSize;

                if (offsetChunk.X < area.Left - ChunkSize || offsetChunk.X > area.Right)
                    continue;

                if (offsetChunk.Y < area.Bottom - ChunkSize || offsetChunk.Y > area.Top)
                    continue;

                foreach (var chunkedLine in chunkedLines)
                {
                    var leftTop = ScalePosition(new Vector2
                        (Math.Min(chunkedLine.Origin.X, chunkedLine.Terminus.X) - 0.1f,
                        Math.Min(chunkedLine.Origin.Y, chunkedLine.Terminus.Y) - 0.1f)
                        - new Vector2(offset.X, -offset.Y));

                    var rightTop = ScalePosition(new Vector2
                        (Math.Max(chunkedLine.Origin.X, chunkedLine.Terminus.X) + 0.1f,
                        Math.Min(chunkedLine.Origin.Y, chunkedLine.Terminus.Y) - 0.1f)
                        - new Vector2(offset.X, -offset.Y));

                    var leftBottom = ScalePosition(new Vector2
                        (Math.Min(chunkedLine.Origin.X, chunkedLine.Terminus.X) - 0.1f,
                        Math.Max(chunkedLine.Origin.Y, chunkedLine.Terminus.Y) + 0.1f)
                        - new Vector2(offset.X, -offset.Y));

                    var rightBottom = ScalePosition(new Vector2
                        (Math.Max(chunkedLine.Origin.X, chunkedLine.Terminus.X) + 0.1f,
                        Math.Max(chunkedLine.Origin.Y, chunkedLine.Terminus.Y) + 0.1f)
                        - new Vector2(offset.X, -offset.Y));

                    if (!pipeVertexUVs.TryGetValue(chunkedLine.Color, out var pipeVertexUV))
                        pipeVertexUV = new ValueList<Vector2>();

                    pipeVertexUV.Add(leftBottom);
                    pipeVertexUV.Add(leftTop);
                    pipeVertexUV.Add(rightBottom);
                    pipeVertexUV.Add(leftTop);
                    pipeVertexUV.Add(rightBottom);
                    pipeVertexUV.Add(rightTop);

                    pipeVertexUVs[chunkedLine.Color] = pipeVertexUV;
                }
            }

            foreach ((var color, var pipeVertexUV) in pipeVertexUVs)
            {
                if (pipeVertexUV.Count > 0)
                    handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, pipeVertexUV.Span, color);
            }
        }
    }

    public Dictionary<Vector2i, List<AtmosMonitoringConsoleLine>>?
        GetDecodedAtmosPipeChunks(Dictionary<Vector2i, AtmosPipeChunk>? chunks, MapGridComponent? grid, int? focusNetId = null)
    {
        if (chunks == null || grid == null)
            return null;

        var decodedOutput = new Dictionary<Vector2i, List<AtmosMonitoringConsoleLine>>();

        var northMask = (ulong)1 << 0;
        var southMask = (ulong)1 << 1;
        var westMask = (ulong)1 << 2;
        var eastMask = (ulong)1 << 3;

        foreach ((var chunkOrigin, var chunk) in chunks)
        {
            var list = new List<AtmosMonitoringConsoleLine>();

            foreach (var ((netId, hexColor), atmosPipeData) in chunk.AtmosPipeData)
            {
                for (var tileIdx = 0; tileIdx < ChunkSize * ChunkSize; tileIdx++)
                {
                    if (atmosPipeData == 0)
                        continue;

                    var mask = (ulong)SharedNavMapSystem.AllDirMask << (tileIdx * SharedNavMapSystem.Directions);

                    if ((atmosPipeData & mask) == 0)
                        continue;

                    var relativeTile = GetTileFromIndex(tileIdx);
                    var tile = (chunk.Origin * ChunkSize + relativeTile) * grid.TileSize;
                    var position = new Vector2(tile.X, -tile.Y);

                    // Get the draw points
                    var lineLongitudinalOrigin = ((atmosPipeData & (northMask << (tileIdx * SharedNavMapSystem.Directions))) > 0) ?
                        new Vector2(grid.TileSize * 0.5f, -grid.TileSize * 1f) : new Vector2(grid.TileSize * 0.5f, -grid.TileSize * 0.5f);

                    var lineLongitudinalTerminus = ((atmosPipeData & (southMask << (tileIdx * SharedNavMapSystem.Directions))) > 0) ?
                        new Vector2(grid.TileSize * 0.5f, -grid.TileSize * 0f) : new Vector2(grid.TileSize * 0.5f, -grid.TileSize * 0.5f);

                    var lineLateralOrigin = ((atmosPipeData & (eastMask << (tileIdx * SharedNavMapSystem.Directions))) > 0) ?
                        new Vector2(grid.TileSize * 1f, -grid.TileSize * 0.5f) : new Vector2(grid.TileSize * 0.5f, -grid.TileSize * 0.5f);

                    var lineLateralTerminus = ((atmosPipeData & (westMask << (tileIdx * SharedNavMapSystem.Directions))) > 0) ?
                        new Vector2(grid.TileSize * 0f, -grid.TileSize * 0.5f) : new Vector2(grid.TileSize * 0.5f, -grid.TileSize * 0.5f);

                    // Determine the correct coloration for the pipe
                    var color = Color.FromHex(hexColor) * Color.LightGray;

                    if (focusNetId != null && focusNetId != netId)
                        color *= Color.DarkGray;

                    // Get the appropriate sRBG color
                    color = GetsRGBColor(color);

                    // Add the draw data
                    var lineLongitudinal = new AtmosMonitoringConsoleLine(position + lineLongitudinalOrigin, position + lineLongitudinalTerminus, color);
                    list.Add(lineLongitudinal);

                    var lineLateral = new AtmosMonitoringConsoleLine(position + lineLateralOrigin, position + lineLateralTerminus, color);
                    list.Add(lineLateral);
                }
            }

            if (list.Count > 0)
                decodedOutput.Add(chunkOrigin, list);
        }

        return decodedOutput;
    }

    public static Vector2i GetTileFromIndex(int index)
    {
        var x = index / ChunkSize;
        var y = index % ChunkSize;
        return new Vector2i(x, y);
    }

    public Color GetsRGBColor(Color color)
    {
        if (!_sRGBLookUp.TryGetValue(color, out var sRGB))
        {
            sRGB = Color.ToSrgb(color);
            _sRGBLookUp[color] = sRGB;
        }

        return sRGB;
    }
}

public struct AtmosMonitoringConsoleLine
{
    public readonly Vector2 Origin;
    public readonly Vector2 Terminus;
    public readonly Color Color;

    public AtmosMonitoringConsoleLine(Vector2 origin, Vector2 terminus, Color color)
    {
        Origin = origin;
        Terminus = terminus;
        Color = color;
    }
}
