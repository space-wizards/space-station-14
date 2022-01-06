using System.Collections.Generic;
using Content.Server.Procedural.Components;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Server.Procedural.Systems;

public partial class WorldChunkSystem
{
    private readonly Queue<(DebrisData, Vector2i)> _debrisLoadQueue = new();

    private readonly Stopwatch _debrisLoadStopwatch = new();

    public void ForceEmptyChunk(Vector2i chunk)
    {
        if (_currLoaded.Contains(chunk))
        {
            Logger.ErrorS("worldgen", "Tried to empty a chunk that's already loaded!");
            return;
        }

        _chunks[chunk] = new WorldChunk()
        {
            Debris = new HashSet<DebrisData>()
        };
    }

    private void LoadChunks()
    {
        foreach (var chunk in _loadQueue)
        {
            if (_chunks.ContainsKey(chunk))
            {
                LoadChunk(chunk);
            }
            else
            {
                MakeChunk(chunk);
                LoadChunk(chunk);
            }
        }
        _loadQueue.Clear();

        _debrisLoadStopwatch.Restart();
        while (_debrisLoadQueue.Count != 0)
        {
            var v = _debrisLoadQueue.Dequeue();
            LoadDebris(v.Item1, v.Item2);
            if (_debrisLoadStopwatch.Elapsed.Milliseconds > _maxDebrisLoadTimeMs)
                break;
        }
    }

    private void LoadChunk(Vector2i chunk)
    {
        foreach (var debris in _chunks[chunk].Debris)
        {
            _debrisLoadQueue.Enqueue((debris, chunk));
        }
    }

    private void LoadDebris(DebrisData debris, Vector2i chunk)
    {
        if (debris.CurrGrid is not null && Exists(debris.CurrGrid))
            return;

        debris.CurrGrid = _debrisGeneration.GenerateDebris(debris.Kind!, debris.Coords);
        var comp = AddComp<WorldManagedComponent>(debris.CurrGrid.Value);
        comp.DebrisData = debris;
        comp.CurrentChunk = chunk;
    }

    private void MakeChunk(Vector2i chunk)
    {
        Logger.DebugS("worldgen", $"Made chunk {chunk}.");
        var offs = (int)((ChunkSize - (_debrisSeparation / 2)) / 2);
        var center = chunk * ChunkSize;
        var topLeft = (-offs, -offs);
        var lowerRight = (offs, offs);
        var debrisPoints = _sampler.SampleRectangle(topLeft, lowerRight, _debrisSeparation);
        var debris = new HashSet<DebrisData>(debrisPoints.Count);

        foreach (var p in debrisPoints)
        {
            var kind = _defaultLayout.Pick();
            if (kind is null)
                continue;

            debris.Add(new DebrisData()
            {
                CurrGrid = null,
                Kind = kind,
                Coords = new MapCoordinates(p + center, WorldMap),
            });
        }

        _chunks[chunk] = new WorldChunk()
        {
            Debris = debris,
        };
    }

    private void UnloadChunks()
    {
        foreach (var chunk in _unloadQueue)
        {
            if (!_chunks.ContainsKey(chunk) || _currLoaded.Contains(chunk))
                continue;

            foreach (var debris in _chunks[chunk].Debris)
            {
                UnloadDebris(debris);
            }
        }
        _unloadQueue.Clear();
    }

    private void UnloadDebris(DebrisData debris)
    {
        if (debris.CurrGrid is not null)
            Del(debris.CurrGrid.Value);
        debris.CurrGrid = null;
    }
}
