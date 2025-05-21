using Content.Client.Pinpointer.UI;
using Content.Shared.Atmos.Components;
using Content.Shared.Pinpointer;
using Robust.Client.Graphics;
using Robust.Shared.Collections;
using Robust.Shared.Map.Components;
using System.Linq;
using System.Numerics;

namespace Content.Client.Atmos.Consoles;

public sealed partial class AtmosMonitoringConsoleNavMapControl : NavMapControl
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public bool ShowPipeNetwork = true;
    public int? FocusNetId = null;

    private const int ChunkSize = 4;

    private readonly Color _basePipeNetColor = Color.LightGray;
    private readonly Color _unfocusedPipeNetColor = Color.DimGray;

    private List<AtmosMonitoringConsoleLine> _atmosPipeNetwork = new();
    private Dictionary<Color, Color> _sRGBLookUp = new Dictionary<Color, Color>();

    // Look up tables for merging continuous lines. Indexed by line color
    private Dictionary<Color, Dictionary<Vector2i, Vector2i>> _horizLines = new();
    private Dictionary<Color, Dictionary<Vector2i, Vector2i>> _horizLinesReversed = new();
    private Dictionary<Color, Dictionary<Vector2i, Vector2i>> _vertLines = new();
    private Dictionary<Color, Dictionary<Vector2i, Vector2i>> _vertLinesReversed = new();

    public AtmosMonitoringConsoleNavMapControl() : base()
    {
        PostWallDrawingAction += DrawAllPipeNetworks;
    }

    protected override void UpdateNavMap()
    {
        base.UpdateNavMap();

        if (!_entManager.TryGetComponent<AtmosMonitoringConsoleComponent>(Owner, out var console))
            return;

        if (!_entManager.TryGetComponent<MapGridComponent>(MapUid, out var grid))
            return;

        _atmosPipeNetwork = GetDecodedAtmosPipeChunks(console.AtmosPipeChunks, grid);
    }

    private void DrawAllPipeNetworks(DrawingHandleScreen handle)
    {
        if (!ShowPipeNetwork)
            return;

        // Draw networks
        if (_atmosPipeNetwork != null && _atmosPipeNetwork.Any())
            DrawPipeNetwork(handle, _atmosPipeNetwork);
    }

    private void DrawPipeNetwork(DrawingHandleScreen handle, List<AtmosMonitoringConsoleLine> atmosPipeNetwork)
    {
        var offset = GetOffset();
        offset = offset with { Y = -offset.Y };

        if (WorldRange / WorldMaxRange > 0.5f)
        {
            var pipeNetworks = new Dictionary<Color, ValueList<Vector2>>();

            foreach (var chunkedLine in atmosPipeNetwork)
            {
                var start = ScalePosition(chunkedLine.Origin - offset);
                var end = ScalePosition(chunkedLine.Terminus - offset);

                if (!pipeNetworks.TryGetValue(chunkedLine.Color, out var subNetwork))
                    subNetwork = new ValueList<Vector2>();

                subNetwork.Add(start);
                subNetwork.Add(end);

                pipeNetworks[chunkedLine.Color] = subNetwork;
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

            foreach (var chunkedLine in atmosPipeNetwork)
            {
                var leftTop = ScalePosition(new Vector2
                    (Math.Min(chunkedLine.Origin.X, chunkedLine.Terminus.X) - 0.1f,
                    Math.Min(chunkedLine.Origin.Y, chunkedLine.Terminus.Y) - 0.1f)
                    - offset);

                var rightTop = ScalePosition(new Vector2
                    (Math.Max(chunkedLine.Origin.X, chunkedLine.Terminus.X) + 0.1f,
                    Math.Min(chunkedLine.Origin.Y, chunkedLine.Terminus.Y) - 0.1f)
                    - offset);

                var leftBottom = ScalePosition(new Vector2
                    (Math.Min(chunkedLine.Origin.X, chunkedLine.Terminus.X) - 0.1f,
                    Math.Max(chunkedLine.Origin.Y, chunkedLine.Terminus.Y) + 0.1f)
                    - offset);

                var rightBottom = ScalePosition(new Vector2
                    (Math.Max(chunkedLine.Origin.X, chunkedLine.Terminus.X) + 0.1f,
                    Math.Max(chunkedLine.Origin.Y, chunkedLine.Terminus.Y) + 0.1f)
                    - offset);

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

            foreach ((var color, var pipeVertexUV) in pipeVertexUVs)
            {
                if (pipeVertexUV.Count > 0)
                    handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, pipeVertexUV.Span, color);
            }
        }
    }

    private List<AtmosMonitoringConsoleLine> GetDecodedAtmosPipeChunks(Dictionary<Vector2i, AtmosPipeChunk>? chunks, MapGridComponent? grid)
    {
        var decodedOutput = new List<AtmosMonitoringConsoleLine>();

        if (chunks == null || grid == null)
            return decodedOutput;

        // Clear stale look up table values 
        _horizLines.Clear();
        _horizLinesReversed.Clear();
        _vertLines.Clear();
        _vertLinesReversed.Clear();

        // Generate masks
        var northMask = (ulong)1 << 0;
        var southMask = (ulong)1 << 1;
        var westMask = (ulong)1 << 2;
        var eastMask = (ulong)1 << 3;

        foreach ((var chunkOrigin, var chunk) in chunks)
        {
            var list = new List<AtmosMonitoringConsoleLine>();

            foreach (var ((netId, hexColor), atmosPipeData) in chunk.AtmosPipeData)
            {
                // Determine the correct coloration for the pipe
                var color = Color.FromHex(hexColor) * _basePipeNetColor;

                if (FocusNetId != null && FocusNetId != netId)
                    color *= _unfocusedPipeNetColor;

                // Get the associated line look up tables
                if (!_horizLines.TryGetValue(color, out var horizLines))
                {
                    horizLines = new();
                    _horizLines[color] = horizLines;
                }

                if (!_horizLinesReversed.TryGetValue(color, out var horizLinesReversed))
                {
                    horizLinesReversed = new();
                    _horizLinesReversed[color] = horizLinesReversed;
                }

                if (!_vertLines.TryGetValue(color, out var vertLines))
                {
                    vertLines = new();
                    _vertLines[color] = vertLines;
                }

                if (!_vertLinesReversed.TryGetValue(color, out var vertLinesReversed))
                {
                    vertLinesReversed = new();
                    _vertLinesReversed[color] = vertLinesReversed;
                }

                // Loop over the chunk
                for (var tileIdx = 0; tileIdx < ChunkSize * ChunkSize; tileIdx++)
                {
                    if (atmosPipeData == 0)
                        continue;

                    var mask = (ulong)SharedNavMapSystem.AllDirMask << tileIdx * SharedNavMapSystem.Directions;

                    if ((atmosPipeData & mask) == 0)
                        continue;

                    var relativeTile = GetTileFromIndex(tileIdx);
                    var tile = (chunk.Origin * ChunkSize + relativeTile) * grid.TileSize;
                    tile = tile with { Y = -tile.Y };

                    // Calculate the draw point offsets
                    var vertLineOrigin = (atmosPipeData & northMask << tileIdx * SharedNavMapSystem.Directions) > 0 ?
                        new Vector2(grid.TileSize * 0.5f, -grid.TileSize * 1f) : new Vector2(grid.TileSize * 0.5f, -grid.TileSize * 0.5f);

                    var vertLineTerminus = (atmosPipeData & southMask << tileIdx * SharedNavMapSystem.Directions) > 0 ?
                        new Vector2(grid.TileSize * 0.5f, -grid.TileSize * 0f) : new Vector2(grid.TileSize * 0.5f, -grid.TileSize * 0.5f);

                    var horizLineOrigin = (atmosPipeData & eastMask << tileIdx * SharedNavMapSystem.Directions) > 0 ?
                        new Vector2(grid.TileSize * 1f, -grid.TileSize * 0.5f) : new Vector2(grid.TileSize * 0.5f, -grid.TileSize * 0.5f);

                    var horizLineTerminus = (atmosPipeData & westMask << tileIdx * SharedNavMapSystem.Directions) > 0 ?
                        new Vector2(grid.TileSize * 0f, -grid.TileSize * 0.5f) : new Vector2(grid.TileSize * 0.5f, -grid.TileSize * 0.5f);

                    // Since we can have pipe lines that have a length of a half tile, 
                    // double the vectors and convert to vector2i so we can merge them
                    AddOrUpdateNavMapLine(ConvertVector2ToVector2i(tile + horizLineOrigin, 2), ConvertVector2ToVector2i(tile + horizLineTerminus, 2), horizLines, horizLinesReversed);
                    AddOrUpdateNavMapLine(ConvertVector2ToVector2i(tile + vertLineOrigin, 2), ConvertVector2ToVector2i(tile + vertLineTerminus, 2), vertLines, vertLinesReversed);
                }
            }
        }

        // Scale the vector2is back down and convert to vector2
        foreach (var (color, horizLines) in _horizLines)
        {
            // Get the corresponding sRBG color
            var sRGB = GetsRGBColor(color);

            foreach (var (origin, terminal) in horizLines)
                decodedOutput.Add(new AtmosMonitoringConsoleLine
                    (ConvertVector2iToVector2(origin, 0.5f), ConvertVector2iToVector2(terminal, 0.5f), sRGB));
        }

        foreach (var (color, vertLines) in _vertLines)
        {
            // Get the corresponding sRBG color
            var sRGB = GetsRGBColor(color);

            foreach (var (origin, terminal) in vertLines)
                decodedOutput.Add(new AtmosMonitoringConsoleLine
                    (ConvertVector2iToVector2(origin, 0.5f), ConvertVector2iToVector2(terminal, 0.5f), sRGB));
        }

        return decodedOutput;
    }

    private Vector2 ConvertVector2iToVector2(Vector2i vector, float scale = 1f)
    {
        return new Vector2(vector.X * scale, vector.Y * scale);
    }

    private Vector2i ConvertVector2ToVector2i(Vector2 vector, float scale = 1f)
    {
        return new Vector2i((int)MathF.Round(vector.X * scale), (int)MathF.Round(vector.Y * scale));
    }

    private Vector2i GetTileFromIndex(int index)
    {
        var x = index / ChunkSize;
        var y = index % ChunkSize;
        return new Vector2i(x, y);
    }

    private Color GetsRGBColor(Color color)
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
