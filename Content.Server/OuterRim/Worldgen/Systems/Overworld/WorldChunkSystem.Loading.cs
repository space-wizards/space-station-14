using System.Linq;
using Content.Server.OuterRim.Worldgen.Components;
using Content.Server.OuterRim.Worldgen.Prototypes;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.OuterRim.Worldgen.Systems.Overworld;

public partial class WorldChunkSystem
{
    private readonly Queue<(DebrisData, Vector2i)> _debrisLoadQueue = new();

    private readonly Stopwatch _debrisLoadStopwatch = new();

    private readonly HashSet<Vector2i> _takenPoIChunks = new();

    public const float MaximumPoILocationMagnitude = 6;

    public Vector2i GetCleanPoILocation()
    {
        Vector2i loc;
        do
        {
            loc = _random.NextVector2(MaximumPoILocationMagnitude).Floored();
        } while (_takenPoIChunks.Contains(loc));

        return loc;
    }

    public void ForceEmptyChunk(Vector2i chunk)
    {
        if (_chunks.ContainsKey(chunk))
        {
            Logger.ErrorS("worldgen", "Tried to empty a chunk that's already loaded!");
            return;
        }

        _chunks[chunk] = new WorldChunk()
        {
            Debris = new HashSet<DebrisData>(),
            Biome = SelectBiome(chunk),
        };
        _safeSpawnLocations.Add(chunk);
    }

    public void ForceSpawnChunk(Vector2i chunk)
    {
        if (_chunks.ContainsKey(chunk))
        {
            Logger.ErrorS("worldgen", "Tried to empty a chunk that's already loaded!");
            return;
        }

        _chunks[chunk] = new WorldChunk()
        {
            Debris = new HashSet<DebrisData>(),
            Biome = _defaultBiome,
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
        if (ShouldClipChunk(chunk))
        {
            ForceEmptyChunk(chunk);
            return;
        }

        if (_random.Prob(_pointOfInterestChance))
        {
            ForceEmptyChunk(chunk);
            var poi = _random.Pick(_prototypeManager.EnumeratePrototypes<PointOfInterestPrototype>().ToList());
            poi.Generator.Generate(chunk);
            return;
        }

        var density = GetChunkDensity(chunk);
        var offs = (int)((ChunkSize - (density / 2)) / 2);
        var center = chunk * ChunkSize;
        var topLeft = (-offs, -offs);
        var lowerRight = (offs, offs);
        var debrisPoints = _sampler.SampleRectangle(topLeft, lowerRight, density);
        var debris = new HashSet<DebrisData>(debrisPoints.Count);
        var biome = SelectBiome(chunk);
        if (biome.DebrisLayouts.Length != 0)
        {
            foreach (var p in debrisPoints)
            {
                var kind = _prototypeManager.Index<DebrisLayoutPrototype>(_random.Pick(biome.DebrisLayouts)).Pick();
                if (kind is null)
                    continue;

                debris.Add(new DebrisData()
                {
                    CurrGrid = null,
                    Kind = kind,
                    Coords = new MapCoordinates(p + center, WorldMap),
                });
            }
        }

        _chunks[chunk] = new WorldChunk()
        {
            Debris = debris,
            Biome = biome
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
