using System.Numerics;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Decals;
using Content.Shared.Maps;
using Robust.Shared.EntitySerialization;
using Robust.Shared.GameStates;
using Robust.Shared.Map.Events;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Content.Server.Decals;

public sealed partial class DecalSystem : SharedDecalSystem
{
    [Dependency] private IAdminManager _adminManager = default!;
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private SharedMapSystem _mapSystem = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private TurfSystem _turf = default!;

    [Dependency] private EntityQuery<MapGridComponent> _gridQuery;

    private static readonly Vector2 _boundsMinExpansion = new(0.01f, 0.01f);
    private static readonly Vector2 _boundsMaxExpansion = new(1.01f, 1.01f);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TileChangedEvent>(OnTileChanged);
        SubscribeLocalEvent<DecalGridComponent, ComponentStartup>(OnLegacyDecalGridStartup);
        SubscribeLocalEvent<BeforeSerializationEvent>(OnBeforeSerialization);
        SubscribeLocalEvent<PostGridSplitEvent>(OnGridSplit);
    }

    private void OnLegacyDecalGridStartup(EntityUid uid, DecalGridComponent component, ComponentStartup args)
    {
        MigrateLegacyDecalGrid(uid, component);
        RemCompDeferred(uid, component);
    }

    private void OnBeforeSerialization(BeforeSerializationEvent ev)
    {
        if (ev.Category != FileCategory.Map &&
            ev.Category != FileCategory.Grid &&
            ev.Category != FileCategory.Save)
        {
            return;
        }

        var migrated = new List<EntityUid>();

        foreach (var uid in ev.Entities)
        {
            if (!TryComp<DecalGridComponent>(uid, out var component))
                continue;

            MigrateLegacyDecalGrid(uid, component);
            RemComp(uid, component);
            migrated.Add(uid);
        }

        foreach (var uid in migrated)
        {
            foreach (var chunk in ChunkEntities.GetChunks(uid))
            {
                ev.Entities.Add(chunk.Owner);
            }
        }
    }

    private void MigrateLegacyDecalGrid(EntityUid uid, DecalGridComponent component)
    {
        // Old maps store grid-wide decal chunks; convert them into chunk entities and remove the legacy component.
        foreach (var chunk in component.ChunkCollection.ChunkCollection.Values)
        {
            foreach (var (id, decal) in chunk.Decals)
            {
                AddDecalWithId(uid, id, decal);
            }
        }
    }

    private void OnGridSplit(ref PostGridSplitEvent ev)
    {
        if (!_gridQuery.HasComp(ev.OldGrid) || !_gridQuery.TryComp(ev.Grid, out var grid))
            return;

        var moved = new HashSet<DecalIndex>();
        var toMove = new List<(DecalIndex Id, Decal Decal)>();

        foreach (var tile in _mapSystem.GetAllTilesEnumerator(ev.Grid, grid))
        {
            var tilePos = (Vector2) tile.GridIndices;
            var bounds = new Box2(tilePos - _boundsMinExpansion, tilePos + _boundsMaxExpansion);

            foreach (var (id, decal) in GetDecalsIntersecting(ev.OldGrid, bounds))
            {
                if (moved.Add(id))
                    toMove.Add((id, decal));
            }
        }

        foreach (var (id, decal) in toMove)
        {
            RemoveDecal(ev.OldGrid, id);
            TryAddDecal(decal, new EntityCoordinates(ev.Grid, decal.Coordinates), out _);
        }
    }

    private void OnTileChanged(ref TileChangedEvent args)
    {
        if (!_gridQuery.HasComp(args.Entity))
            return;

        var toDelete = new HashSet<DecalIndex>();

        foreach (var change in args.Changes)
        {
            if (!_turf.IsSpace(change.NewTile))
                continue;

            var tilePos = (Vector2) change.GridIndices;
            var bounds = new Box2(tilePos, tilePos + Vector2.One);

            foreach (var (id, _) in GetDecalsIntersecting(args.Entity, bounds))
            {
                toDelete.Add(id);
            }
        }

        foreach (var id in toDelete)
        {
            RemoveDecal(args.Entity, id);
        }
    }

    protected override void OnDecalPlacementRequest(RequestDecalPlacementEvent ev, EntitySessionEventArgs eventArgs)
    {
        if (eventArgs.SenderSession is not { } session)
            return;

        if (!_adminManager.HasAdminFlag(session, AdminFlags.Spawn))
            return;

        var coordinates = GetCoordinates(ev.Coordinates);

        if (!coordinates.IsValid(EntityManager))
            return;

        if (!TryAddDecal(ev.Decal, coordinates, out _))
            return;

        if (eventArgs.SenderSession.AttachedEntity != null)
        {
            _adminLogger.Add(LogType.CrayonDraw, LogImpact.Low,
                $"{ToPrettyString(eventArgs.SenderSession.AttachedEntity.Value):actor} drew a {ev.Decal.Color} {ev.Decal.Id} at {ev.Coordinates}");
        }
        else
        {
            _adminLogger.Add(LogType.CrayonDraw, LogImpact.Low,
                $"{eventArgs.SenderSession.Name} drew a {ev.Decal.Color} {ev.Decal.Id} at {ev.Coordinates}");
        }
    }

    protected override void OnDecalRemovalRequest(RequestDecalRemovalEvent ev, EntitySessionEventArgs eventArgs)
    {
        if (eventArgs.SenderSession is not { } session)
            return;

        if (!_adminManager.HasAdminFlag(session, AdminFlags.Spawn))
            return;

        var coordinates = GetCoordinates(ev.Coordinates);

        if (!coordinates.IsValid(EntityManager))
            return;

        var gridId = _transform.GetGrid(coordinates);

        if (gridId == null)
            return;

        foreach (var (decalId, decal) in GetDecalsInRange(gridId.Value, ev.Coordinates.Position))
        {
            if (eventArgs.SenderSession.AttachedEntity != null)
            {
                _adminLogger.Add(LogType.CrayonDraw, LogImpact.Low,
                    $"{ToPrettyString(eventArgs.SenderSession.AttachedEntity.Value):actor} removed a {decal.Color} {decal.Id} at {ev.Coordinates}");
            }
            else
            {
                _adminLogger.Add(LogType.CrayonDraw, LogImpact.Low,
                    $"{eventArgs.SenderSession.Name} removed a {decal.Color} {decal.Id} at {ev.Coordinates}");
            }

            RemoveDecal(gridId.Value, decalId);
        }
    }

    public bool TryAddDecal(string id, EntityCoordinates coordinates, out DecalIndex decalId, Color? color = null, Angle? rotation = null, int zIndex = 0, bool cleanable = false)
    {
        rotation ??= Angle.Zero;
        var decal = new Decal(coordinates.Position, id, color, rotation.Value, zIndex, cleanable);

        return TryAddDecal(decal, coordinates, out decalId);
    }

    public bool TryAddDecal(Decal decal, EntityCoordinates coordinates, out DecalIndex decalId)
    {
        decalId = default;

        if (!ProtoMan.HasIndex<DecalPrototype>(decal.Id))
            return false;

        var gridId = _transform.GetGrid(coordinates);
        if (gridId == null || !_gridQuery.TryComp(gridId.Value, out var grid))
            return false;

        if (_turf.IsSpace(_mapSystem.GetTileRef(gridId.Value, grid, coordinates)))
            return false;

        var chunk = GetOrCreateDecalChunk(gridId.Value, decal.Coordinates);
        if (!TryAllocateDecalId(chunk, out var chunkDecalId))
            return false;

        AddDecalWithId(chunk, chunkDecalId, decal);
        decalId = new DecalIndex(chunk.Comp1.Chunk, chunkDecalId);
        return true;
    }

    public override bool RemoveDecal(EntityUid gridId, DecalIndex decal)
        => RemoveDecalInternal(gridId, decal, out _);

    public bool RemoveDecal(EntityUid gridId, Vector2i chunkIndices, ushort decalId)
        => RemoveDecalInternal(gridId, chunkIndices, decalId, out _);

    private bool RemoveDecalInternal(EntityUid gridId, DecalIndex decal, out Decal? removed)
        => RemoveDecalInternal(gridId, decal.Chunk, decal.Id, out removed);

    private bool RemoveDecalInternal(EntityUid gridId, Vector2i chunkIndices, ushort decalId, out Decal? removed)
    {
        removed = null;

        // Biomes remember the chunk that owns a decal so unloading does not need a grid-wide decal-id scan.
        if (!ChunkEntities.TryGetChunk(gridId, chunkIndices, out var chunkEnt) ||
            !DecalChunkQuery.TryComp(chunkEnt.Value.Owner, out var decals) ||
            !decals.Decals.Remove(decalId, out removed))
        {
            return false;
        }

        FreeDecalId(decals, decalId);
        DirtyChunk((chunkEnt.Value.Owner, chunkEnt.Value.Comp, decals));
        return true;
    }

    /// <summary>
    /// Changes a decal's position. Note this will actually result in a new decal being created, possibly on a new grid or chunk.
    /// </summary>
    /// <remarks>
    /// If the new position is invalid, this will result in the decal getting deleted.
    /// </remarks>
    public bool SetDecalPosition(EntityUid gridId, DecalIndex decalId, EntityCoordinates coordinates)
    {
        if (!RemoveDecalInternal(gridId, decalId, out var removed))
            return false;

        return TryAddDecal(removed!.WithCoordinates(coordinates.Position), coordinates, out _);
    }

    private bool ModifyDecal(EntityUid gridId, DecalIndex decalId, Func<Decal, Decal> modifyDecal)
    {
        if (!TryGetDecalChunk(gridId, decalId, out var chunk))
            return false;

        chunk.Comp2.Decals[decalId.Id] = modifyDecal(chunk.Comp2.Decals[decalId.Id]);
        DirtyChunk(chunk);
        return true;
    }

    public bool SetDecalColor(EntityUid gridId, DecalIndex decalId, Color? value)
        => ModifyDecal(gridId, decalId, x => x.WithColor(value));

    public bool SetDecalRotation(EntityUid gridId, DecalIndex decalId, Angle value)
        => ModifyDecal(gridId, decalId, x => x.WithRotation(value));

    public bool SetDecalZIndex(EntityUid gridId, DecalIndex decalId, int value)
        => ModifyDecal(gridId, decalId, x => x.WithZIndex(value));

    public bool SetDecalCleanable(EntityUid gridId, DecalIndex decalId, bool value)
        => ModifyDecal(gridId, decalId, x => x.WithCleanable(value));

    public bool SetDecalId(EntityUid gridId, DecalIndex decalId, string id)
    {
        if (!ProtoMan.HasIndex<DecalPrototype>(id))
            throw new ArgumentOutOfRangeException($"Tried to set decal id to invalid prototypeid: {id}");

        return ModifyDecal(gridId, decalId, x => x.WithId(id));
    }

    private void AddDecalWithId(EntityUid gridUid, ushort id, Decal decal)
    {
        var chunk = GetOrCreateDecalChunk(gridUid, decal.Coordinates);
        AddDecalWithId(chunk, id, decal);
    }

    private void AddDecalWithId(Entity<ChunkEntityComponent, DecalChunkComponent> chunk, ushort id, Decal decal)
    {
        chunk.Comp2.Decals[id] = decal;

        if (id <= DecalChunkComponent.MaxServerDecalId)
        {
            chunk.Comp2.MaxDecalId = Math.Max(chunk.Comp2.MaxDecalId, id);
            chunk.Comp2.FreeDecalIds.Remove(id);
        }

        DirtyChunk(chunk);
    }

    private Entity<ChunkEntityComponent, DecalChunkComponent> GetOrCreateDecalChunk(EntityUid gridUid, Vector2 coordinates)
    {
        var chunk = ChunkEntities.GetOrCreateChunk(gridUid, ChunkEntitySystem.GetChunkIndices(coordinates));
        return (chunk.Owner, chunk.Comp, EnsureComp<DecalChunkComponent>(chunk.Owner));
    }

    private bool TryGetDecalChunk(EntityUid gridUid, DecalIndex decalId, out Entity<ChunkEntityComponent, DecalChunkComponent> chunk)
    {
        if (ChunkEntities.TryGetChunk(gridUid, decalId.Chunk, out var chunkEnt) &&
            DecalChunkQuery.TryComp(chunkEnt.Value.Owner, out var decals) &&
            decals.Decals.ContainsKey(decalId.Id))
        {
            chunk = (chunkEnt.Value.Owner, chunkEnt.Value.Comp, decals);
            return true;
        }

        chunk = default;
        return false;
    }

    private bool TryAllocateDecalId(Entity<ChunkEntityComponent, DecalChunkComponent> chunk, out ushort decalId)
    {
        // We'll recycle decal IDs so we don't overflow.
        for (var i = chunk.Comp2.FreeDecalIds.Count - 1; i >= 0; i--)
        {
            var free = chunk.Comp2.FreeDecalIds[i];
            chunk.Comp2.FreeDecalIds.RemoveAt(i);

            // Sanity check to avoid overlap.
            if (free > DecalChunkComponent.MaxServerDecalId || chunk.Comp2.Decals.ContainsKey(free))
                continue;

            decalId = free;
            return true;
        }

        while (true)
        {
            ushort next;
            if (chunk.Comp2.Decals.Count == 0 && chunk.Comp2.MaxDecalId == 0)
            {
                next = 0;
            }
            else
            {
                // Too many decals sorry!
                if (chunk.Comp2.MaxDecalId >= DecalChunkComponent.MaxServerDecalId)
                    break;

                next = (ushort) (chunk.Comp2.MaxDecalId + 1);
            }

            if (chunk.Comp2.Decals.ContainsKey(next))
            {
                chunk.Comp2.MaxDecalId = next;
                continue;
            }

            decalId = next;
            return true;
        }

        decalId = default;
        return false;
    }

    private static void FreeDecalId(DecalChunkComponent chunk, ushort decalId)
    {
        if (decalId > DecalChunkComponent.MaxServerDecalId || chunk.FreeDecalIds.Contains(decalId))
            return;

        chunk.FreeDecalIds.Add(decalId);
        SortFreeDecalIds(chunk);
    }

    private static void SortFreeDecalIds(DecalChunkComponent chunk)
    {
        // Allocation uses RemoveAt(Count - 1), so descending order pops the lowest id first.
        chunk.FreeDecalIds.Sort((x, y) => y.CompareTo(x));
    }

    private void DirtyChunk(Entity<ChunkEntityComponent, DecalChunkComponent> chunk)
    {
        if (chunk.Comp2.Decals.Count == 0)
        {
            RemComp(chunk.Owner, chunk.Comp2);
            ChunkEntities.TryRemoveChunk((chunk.Owner, chunk.Comp1, MetaData(chunk.Owner)));
            return;
        }

        DirtyField(chunk.Owner, chunk.Comp2, nameof(DecalChunkComponent.Decals));
    }

}
