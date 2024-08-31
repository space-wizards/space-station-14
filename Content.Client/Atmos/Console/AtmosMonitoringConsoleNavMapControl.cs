using Content.Client.Pinpointer.UI;
using Content.Shared.Atmos.Components;
using Content.Shared.Pinpointer;
using Robust.Client.Graphics;
using Robust.Shared.Collections;
using Robust.Shared.Map.Components;
using System.Numerics;

namespace Content.Client.Atmos.Console;

public sealed partial class AtmosMonitoringConsoleNavMapControl : NavMapControl
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    private Dictionary<Color, Color> _sRGBLookUp = new Dictionary<Color, Color>();

    public Dictionary<Vector2i, List<AtmosMonitoringConsoleLine>>? AtmosPipeNetwork;
    public bool ShowPipeNetwork = true;
    private MapGridComponent? _grid;

    public AtmosMonitoringConsoleNavMapControl() : base()
    {
        // Set colors
        //WallColor = new Color(180, 145, 0);
        //WallColor = new Color(0, 69, 40);
        //WallColor = new Color(0, 176, 102);
        WallColor = new Color(64, 64, 64);
        TileColor = Color.DimGray * WallColor;

        //_backgroundColor = Color.FromSrgb(TileColor.WithAlpha(_backgroundOpacity));

        PostWallDrawingAction += DrawAllPipeNetworks;
    }

    protected override void UpdateNavMap()
    {
        base.UpdateNavMap();

        if (Owner == null)
            return;

        if (!_entManager.TryGetComponent<AtmosMonitoringConsoleComponent>(Owner, out var console))
            return;

        if (!_entManager.TryGetComponent(MapUid, out _grid))
            return;

        AtmosPipeNetwork = GetDecodedAtmosPipeChunks(console.AtmosPipeChunks, _grid);
    }

    public void DrawAllPipeNetworks(DrawingHandleScreen handle)
    {
        // Draw network
        if (AtmosPipeNetwork != null && AtmosPipeNetwork.Count > 0 && ShowPipeNetwork)
        {
            DrawPipeNetwork(handle, AtmosPipeNetwork);
        }
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
                {
                    if (!_sRGBLookUp.TryGetValue(color, out var sRGB))
                    {
                        sRGB = Color.ToSrgb(color);
                        _sRGBLookUp[color] = sRGB;
                    }

                    handle.DrawPrimitives(DrawPrimitiveTopology.LineList, subNetwork.Span, sRGB);
                }
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
                {
                    if (!_sRGBLookUp.TryGetValue(color, out var sRGB))
                    {
                        sRGB = Color.ToSrgb(color);
                        _sRGBLookUp[color] = sRGB;
                    }

                    handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, pipeVertexUV.Span, sRGB);
                }
            }
        }
    }

    public static int ChunkSize = 4;

    public static Vector2i GetTileFromIndex(int index)
    {
        var x = index / ChunkSize;
        var y = index % ChunkSize;
        return new Vector2i(x, y);
    }

    public Dictionary<Vector2i, List<AtmosMonitoringConsoleLine>>? GetDecodedAtmosPipeChunks(Dictionary<Vector2i, AtmosPipeChunk>? chunks, MapGridComponent? grid)
    {
        if (chunks == null || grid == null)
            return null;

        var decodedOutput = new Dictionary<Vector2i, List<AtmosMonitoringConsoleLine>>();

        foreach ((var chunkOrigin, var chunk) in chunks)
        {
            var list = new List<AtmosMonitoringConsoleLine>();

            foreach ((var hexColor, var atmosPipeData) in chunk.AtmosPipeData)
            {
                for (var chunkIdx = 0; chunkIdx < ChunkSize * ChunkSize; chunkIdx++)
                {
                    var value = (int)Math.Pow(2, chunkIdx);

                    var northMask = atmosPipeData.NorthFacing & value;
                    var southMask = atmosPipeData.SouthFacing & value;
                    var eastMask = atmosPipeData.EastFacing & value;
                    var westMask = atmosPipeData.WestFacing & value;

                    if ((northMask | southMask | eastMask | westMask) == 0)
                        continue;

                    var relativeTile = GetTileFromIndex(chunkIdx);
                    var tile = (chunk.Origin * ChunkSize + relativeTile) * grid.TileSize;
                    var position = new Vector2(tile.X, -tile.Y);

                    var lineLongitudinalOrigin = (northMask > 0) ?
                        new Vector2(grid.TileSize * 0.5f, -grid.TileSize * 1f) : new Vector2(grid.TileSize * 0.5f, -grid.TileSize * 0.5f);
                    var lineLongitudinalTerminus = (southMask > 0) ?
                        new Vector2(grid.TileSize * 0.5f, -grid.TileSize * 0f) : new Vector2(grid.TileSize * 0.5f, -grid.TileSize * 0.5f);
                    var lineLateralOrigin = (eastMask > 0) ?
                        new Vector2(grid.TileSize * 1f, -grid.TileSize * 0.5f) : new Vector2(grid.TileSize * 0.5f, -grid.TileSize * 0.5f);
                    var lineLateralTerminus = (westMask > 0) ?
                        new Vector2(grid.TileSize * 0f, -grid.TileSize * 0.5f) : new Vector2(grid.TileSize * 0.5f, -grid.TileSize * 0.5f);

                    // Add points
                    var color = Color.FromHex(hexColor) * Color.DarkGray;

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
