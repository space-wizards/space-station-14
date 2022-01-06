using System.Collections.Generic;
using System.Linq;
using Content.Server.Ghost.Components;
using Content.Server.Procedural.Components;
using Content.Server.Procedural.Prototypes;
using Content.Server.Procedural.Tools;
using Content.Shared.CCVar;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Procedural.Systems;

public partial class WorldChunkSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly PoissonDiskSampler _sampler = default!;
    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly DebrisGenerationSystem _debrisGeneration = default!;

    public const int WorldLoadRadius = 256;
    public const int ChunkSize = 128;
    private readonly Dictionary<Vector2i, WorldChunk> _chunks = new();
    public MapId WorldMap = MapId.Nullspace;
    private readonly Queue<Vector2i> _loadQueue = new();
    private readonly Queue<Vector2i> _unloadQueue = new();
    private HashSet<Vector2i> _currLoaded = new();
    private float _frameAccumulator = 0.0f;

    // CVar replicas
    private float _debrisSeparation;
    private DebrisLayoutPrototype _defaultLayout = default!;
    private bool _enabled;
    private float _maxDebrisLoadTimeMs;

    public override void Initialize()
    {
        _configuration.OnValueChanged(CCVars.MinDebrisSeparation, f => _debrisSeparation = f, true);
        _configuration.OnValueChanged(CCVars.SpawnDebrisLayout, l =>
        {
            _defaultLayout = _prototypeManager.Index<DebrisLayoutPrototype>(l);
        }, true);
        _configuration.OnValueChanged(CCVars.WorldGenEnabled, e => _enabled = e, true);
        _configuration.OnValueChanged(CCVars.MaxDebrisLoadTimeMs, t => _maxDebrisLoadTimeMs = t, true);

        SubscribeLocalEvent<WorldManagedComponent, MoveEvent>(OnDebrisMoved);
    }

    private void OnDebrisMoved(EntityUid uid, WorldManagedComponent component, ref MoveEvent args)
    {
        var chunk = (Transform(uid).MapPosition.Position / ChunkSize).Rounded().Floored();
        if (component.DebrisData is null)
            return; // AllComponentsOneEntityDeleteTest moment.
        component.DebrisData.Coords = Transform(uid).MapPosition;
        if (component.CurrentChunk == chunk)
            return;

        var old = component.CurrentChunk;
        component.CurrentChunk = chunk;
        _chunks[old].Debris.Remove(component.DebrisData);
        _chunks[chunk].Debris.Add(component.DebrisData);
        if (!_currLoaded.Contains(chunk))
            UnloadDebris(component.DebrisData);
    }

    //TODO: Optimization pass over EVERYTHING here. This is one of the most performance sensitive parts of OR14!
    public override void Update(float frameTime)
    {
        if (!_enabled)
            return;

        _frameAccumulator += frameTime;

        if (!(_frameAccumulator > 1.0f / 3.0f))
            return;
        _frameAccumulator -= 1.0f / 3.0f;

        UpdateWorldLoadState(); // Repopulate the load queue and unload queue; ensure _currLoad is up to date.
        LoadChunks(); // Load everything that needs loaded.
        UnloadChunks(); // Unload chunks outside of view.
    }

    public void Reset()
    {
        _chunks.Clear();
        _loadQueue.Clear();
        _unloadQueue.Clear();
        _currLoaded.Clear();
    }

    private void UpdateWorldLoadState()
    {
        var lastLoaded = _currLoaded;
        _currLoaded = new HashSet<Vector2i>(lastLoaded.Count);

        var players = _playerManager.ServerSessions.Where(x =>
            x.AttachedEntity is not null && // Must have an entity.
            !HasComp<GhostComponent>(x.AttachedEntity.Value));

        // Find all chunks that should be loaded
        foreach (var player in players)
        {
            foreach (var chunk in ChunksNear(player.AttachedEntity!.Value))
            {
                _currLoaded.Add(chunk);
            }
        }

        //Find what we need to load, and what we need to unload.
        var toLoad = _currLoaded.ToHashSet();
        toLoad.ExceptWith(lastLoaded);
        lastLoaded.ExceptWith(_currLoaded);

        foreach (var v in lastLoaded)
        {
            _unloadQueue.Enqueue(v);
        }

        foreach (var v in toLoad)
        {
            Logger.DebugS("worldgen", $"Loading chunk {v}.");
            _loadQueue.Enqueue(v);
        }
    }

    public IEnumerable<Vector2i> ChunksNear(EntityUid ent)
    {
        var offs = Transform(ent).WorldPosition.Floored() / ChunkSize;
        const int division = (WorldLoadRadius / ChunkSize) + 1;
        for (var x = -division; x <= division; x+=1)
        {
            for (var y = -division; y <= division; y+=1)
            {
                if (x * x + y * y <= division * division)
                {
                    yield return offs + (x, y);
                }
            }
        }
    }
}

public struct WorldChunk
{
    public HashSet<DebrisData> Debris;
}

public class DebrisData
{
    public DebrisPrototype? Kind;
    public MapCoordinates Coords;
    public EntityUid? CurrGrid;
}
