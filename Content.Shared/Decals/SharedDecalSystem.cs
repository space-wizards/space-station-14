using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared.Maps;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Administration.Logs;
using Content.Shared.Administration.Managers;
using Robust.Shared;
using Robust.Shared.Player;
using Robust.Shared.GameStates;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.Network;
using static Content.Shared.Decals.DecalGridComponent;
using ChunkIndicesEnumerator = Robust.Shared.Map.Enumerators.ChunkIndicesEnumerator;

namespace Content.Shared.Decals;

public abstract class SharedDecalSystem : EntitySystem
{
    [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
    [Dependency] protected readonly IMapManager MapManager = default!;
    [Dependency] protected readonly SharedMapSystem MapSystem = default!;
    [Dependency] protected readonly SharedTransformSystem TransformSystem = default!;
    [Dependency] private readonly IConfigurationManager _conf = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly TurfSystem Turf = default!;
    [Dependency] private readonly ISharedAdminManager _adminManager = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly INetManager _net = default!;

    protected bool PvsEnabled;

    protected readonly Dictionary<ICommonSession, Dictionary<NetEntity, HashSet<Vector2i>>> PreviousSentChunks = new();

    // Note that this constant is effectively baked into all map files, because of how they save the grid decal component.
    // So if this ever needs changing, the maps need converting.
    public const int ChunkSize = 32;
    public static Vector2i GetChunkIndices(Vector2 coordinates) => new ((int) Math.Floor(coordinates.X / ChunkSize), (int) Math.Floor(coordinates.Y / ChunkSize));

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TileChangedEvent>(OnTileChanged);
        SubscribeNetworkEvent<RequestDecalPlacementEvent>(OnDecalPlacementRequest);
        SubscribeLocalEvent<GridInitializeEvent>(OnGridInitialize);
        SubscribeLocalEvent<DecalGridComponent, ComponentStartup>(OnCompStartup);
        SubscribeLocalEvent<DecalGridComponent, ComponentGetState>(OnGetState);
        SubscribeNetworkEvent<RequestDecalRemovalEvent>(OnDecalRemovalRequest);

        Subs.CVar(_conf, CVars.NetPVS, OnPvsToggle, true);
    }

    private void OnPvsToggle(bool value)
    {
        if (value == PvsEnabled)
            return;

        PvsEnabled = value;

        if (value)
            return;

        foreach (var playerData in PreviousSentChunks.Values)
        {
            playerData.Clear();
        }

        var query = AllEntityQuery<DecalGridComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out var grid, out var meta))
        {
            grid.ForceTick = Timing.CurTick;
            Dirty(uid, grid, meta);
        }
    }


    private void OnDecalRemovalRequest(RequestDecalRemovalEvent ev, EntitySessionEventArgs eventArgs)
    {
        if (eventArgs.SenderSession is not { } session)
            return;

        // bad
        if (!_adminManager.HasAdminFlag(session, AdminFlags.Spawn))
            return;

        var coordinates = GetCoordinates(ev.Coordinates);

        if (!coordinates.IsValid(EntityManager))
            return;

        var gridId = TransformSystem.GetGrid(coordinates);

        if (gridId == null)
            return;

        // remove all decals on the same tile
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

            if (ev.Decal != null && decal != ev.Decal)
                continue;

            RemoveDecal(gridId.Value, decalId, out var _);
        }
    }

    private void OnTileChanged(ref TileChangedEvent args)
    {
        if (!TryComp(args.Entity, out DecalGridComponent? grid))
            return;

        var toDelete = new HashSet<uint>();

        foreach (var change in args.Changes)
        {
            if (!Turf.IsSpace(change.NewTile))
                continue;

            var indices = GetChunkIndices(change.GridIndices);

            if (!grid.ChunkCollection.ChunkCollection.TryGetValue(indices, out var chunk))
                continue;

            toDelete.Clear();

            foreach (var (uid, decal) in chunk.Decals)
            {
                if (new Vector2((int)Math.Floor(decal.Coordinates.X), (int)Math.Floor(decal.Coordinates.Y)) ==
                    change.GridIndices)
                {
                    toDelete.Add(uid);
                }
            }

            if (toDelete.Count == 0)
                continue;

            foreach (var decalId in toDelete)
            {
                grid.DecalIndex.Remove(decalId);
                chunk.Decals.Remove(decalId);
            }

            DirtyChunk(args.Entity, indices, chunk);
            if (chunk.Decals.Count == 0)
                grid.ChunkCollection.ChunkCollection.Remove(indices);
        }
    }

    private void OnDecalPlacementRequest(RequestDecalPlacementEvent ev, EntitySessionEventArgs eventArgs)
    {
        if (eventArgs.SenderSession is not { } session)
            return;

        // bad
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

    private void OnGetState(EntityUid uid, DecalGridComponent component, ref ComponentGetState args)
    {
        if (PvsEnabled && !args.ReplayState)
            return;

        // Should this be a full component state or a delta-state?
        if (args.FromTick <= component.CreationTick || args.FromTick <= component.ForceTick)
        {
            args.State = new DecalGridState(component.ChunkCollection.ChunkCollection);
            return;
        }

        var data = new Dictionary<Vector2i, DecalChunk>();
        foreach (var (index, chunk) in component.ChunkCollection.ChunkCollection)
        {
            if (chunk.LastModified >= args.FromTick)
                data[index] = chunk;
        }

        args.State = new DecalGridDeltaState(data, new(component.ChunkCollection.ChunkCollection.Keys));
    }

    private void OnGridInitialize(GridInitializeEvent msg)
    {
        EnsureComp<DecalGridComponent>(msg.EntityUid);
    }

    private void OnCompStartup(EntityUid uid, DecalGridComponent component, ComponentStartup args)
    {
        foreach (var (indices, decals) in component.ChunkCollection.ChunkCollection)
        {
            foreach (var decalUid in decals.Decals.Keys)
            {
                component.DecalIndex[decalUid] = indices;
            }
        }

        // This **shouldn't** be required, but just in case we ever get entity prototypes that have decal grids, we
        // need to ensure that we send an initial full state to players.
        Dirty(uid, component);
    }

    protected abstract void DirtyChunk(EntityUid id, Vector2i chunkIndices, DecalChunk chunk);

    private bool ModifyDecal(Entity<DecalGridComponent?> ent, uint decalId, Func<Decal, Decal> modifyDecal)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        if (!ent.Comp.DecalIndex.TryGetValue(decalId, out var indices))
            return false;

        var chunk = ent.Comp.ChunkCollection.ChunkCollection[indices];
        var decal = chunk.Decals[decalId];
        chunk.Decals[decalId] = modifyDecal(decal);
        DirtyChunk(ent.Owner, indices, chunk);
        return true;
    }

    public bool SetDecalColor(Entity<DecalGridComponent?> ent, uint decalId, Color? value)
        => ModifyDecal(ent, decalId, x => x.WithColor(value));

    public bool SetDecalRotation(Entity<DecalGridComponent?> ent, uint decalId, Angle value)
        => ModifyDecal(ent, decalId, x => x.WithRotation(value));

    public bool SetDecalZIndex(Entity<DecalGridComponent?> ent, uint decalId, int value)
        => ModifyDecal(ent, decalId, x => x.WithZIndex(value));

    public bool SetDecalCleanable(Entity<DecalGridComponent?> ent, uint decalId, bool value)
        => ModifyDecal(ent, decalId, x => x.WithCleanable(value));

    public bool SetDecalId(Entity<DecalGridComponent?> ent, uint decalId, string id)
    {
        if (!PrototypeManager.HasIndex<DecalPrototype>(id))
            throw new ArgumentOutOfRangeException($"Tried to set decal id to invalid prototypeid: {id}");

        return ModifyDecal(ent, decalId, x => x.WithId(id));
    }

    /// <summary>
    /// Returns key, value pairs of decals
    /// </summary>
    /// <remarks>
    /// Returned values should not be stored, as they might get invalidated from incoming server message
    /// </remarks>
    public HashSet<(uint Index, Decal Decal)> GetDecalsInRange(Entity<DecalGridComponent?> ent, Vector2 position, float distance = 0.75f, Func<Decal, bool>? validDelegate = null)
    {
        var decalIds = new HashSet<(uint, Decal)>();

        if (!Resolve(ent.Owner, ref ent.Comp))
            return decalIds;

        var chunkIndices = GetChunkIndices(position);

        if (ent.Comp.ChunkCollection.ChunkCollection == null)
            return decalIds;

        if (!ent.Comp.ChunkCollection.ChunkCollection.TryGetValue(chunkIndices, out var chunk))
            return decalIds;

        foreach (var (uid, decal) in chunk.Decals)
        {
            if ((position - decal.Coordinates - new Vector2(0.5f, 0.5f)).Length() > distance)
                continue;

            if (chunk.PredictedDecalDeletions.Contains(decal))
                continue;

            if (validDelegate == null || validDelegate(decal))
            {
                decalIds.Add((uid, decal));
            }
        }

        foreach (var (decal, uid) in chunk.PredictedDecals)
        {
            if ((position - decal.Coordinates - new Vector2(0.5f, 0.5f)).Length() > distance)
                continue;

            if (validDelegate == null || validDelegate(decal))
            {
                decalIds.Add((uid, decal));
            }
        }

        return decalIds;
    }

    /// <summary>
    /// Returns key, value pairs of decals
    /// </summary>
    /// <remarks>
    /// Returned values should not be stored, as they might get invalidated from incoming server message
    /// </remarks>
    public HashSet<(uint Index, Decal Decal)> GetDecalsIntersecting(Entity<DecalGridComponent?> ent, Box2 bounds)
    {
        var decalIds = new HashSet<(uint, Decal)>();

        if (!Resolve(ent.Owner, ref ent.Comp))
            return decalIds;

        if (ent.Comp.ChunkCollection.ChunkCollection == null)
            return decalIds;

        var chunks = new ChunkIndicesEnumerator(bounds, ChunkSize);

        while (chunks.MoveNext(out var chunkOrigin))
        {
            if (!ent.Comp.ChunkCollection.ChunkCollection.TryGetValue(chunkOrigin.Value, out var chunk))
                continue;

            foreach (var (id, decal) in chunk.Decals)
            {
                if (!bounds.Contains(decal.Coordinates))
                    continue;

                if (chunk.PredictedDecalDeletions.Contains(decal))
                    continue;

                decalIds.Add((id, decal));
            }

            foreach (var (decal, id) in chunk.PredictedDecals)
            {
                if (!bounds.Contains(decal.Coordinates))
                    continue;

                decalIds.Add((id, decal));
            }
        }

        return decalIds;
    }

    /// <summary>
    ///     Changes a decals position. Note this will actually result in a new decal being created, possibly on a new grid or chunk.
    /// </summary>
    /// <remarks>
    ///     If the new position is invalid, this will result in the decal getting deleted.
    /// </remarks>
    public bool SetDecalPosition(Entity<DecalGridComponent?> ent, uint decalId, EntityCoordinates coordinates)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        if (!RemoveDecal(ent, decalId, out var removed))
            return false;

        return TryAddDecal(removed.WithCoordinates(coordinates.Position), coordinates, out _);
    }

    /// <summary>
    /// Tries to add provided decal at given coordinates,
    /// </summary>
    public bool RemoveDecal(Entity<DecalGridComponent?> ent, uint decalId, [NotNullWhen(true)] out Decal? removed)
    {
        removed = null;

        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (!ent.Comp.DecalIndex.Remove(decalId, out var indices))
            return false;

        if (!ent.Comp.ChunkCollection.ChunkCollection.TryGetValue(indices, out var chunk))
            return false;

        if (!chunk.Decals.Remove(decalId, out removed))
            return false;

        chunk.PredictedDecalDeletions.Add(removed);
        chunk.PredictedDecals.Remove(removed);

        if (chunk.Decals.Count == 0)
            ent.Comp.ChunkCollection.ChunkCollection.Remove(indices);

        DirtyChunk(ent.Owner, indices, chunk);
        return true;
    }

    /// <summary>
    /// Tries to add provided decal at given coordinates,
    /// </summary>
    /// <remarks>
    /// On client returned decalId should not be stored anywhere as it will get replaced by the incoming server message
    /// </remarks>
    public bool TryAddDecal(Decal decal, EntityCoordinates coordinates, [NotNullWhen(true)] out uint? decalId)
    {
        decalId = 0;

        if (!PrototypeManager.HasIndex<DecalPrototype>(decal.Id))
            return false;

        var gridId = TransformSystem.GetGrid(coordinates);

        if (!TryComp(gridId, out MapGridComponent? grid))
            return false;

        if (Turf.IsSpace(MapSystem.GetTileRef(gridId.Value, grid, coordinates)))
            return false;

        if (!TryComp(gridId, out DecalGridComponent? comp))
            return false;

        decal.Coordinates = coordinates.Position;
        decalId = comp.ChunkCollection.NextDecalId++;

        var chunkIndices = GetChunkIndices(decal.Coordinates);
        var chunk = comp.ChunkCollection.ChunkCollection.GetOrNew(chunkIndices);

        if (_net.IsServer)
            chunk.Decals[decalId.Value] = decal;
        else
        {
            // Set the most significant bit of ID, so we know it is being predcited
            decalId |= uint.MaxValue ^ (uint.MaxValue >> 1);
            chunk.PredictedDecals.Add(decal, decalId.Value);
        }
        comp.DecalIndex[decalId.Value] = chunkIndices;
        DirtyChunk(gridId.Value, chunkIndices, chunk);

        return true;
    }
}

/// <summary>
///     Sent by clients to request that a decal is placed on the server.
/// </summary>
[Serializable, NetSerializable]
public sealed class RequestDecalPlacementEvent : EntityEventArgs
{
    public Decal Decal;
    public NetCoordinates Coordinates;

    public RequestDecalPlacementEvent(Decal decal, NetCoordinates coordinates)
    {
        Decal = decal;
        Coordinates = coordinates;
    }
}

[Serializable, NetSerializable]
public sealed class RequestDecalRemovalEvent : EntityEventArgs
{
    public NetCoordinates Coordinates;
    public Decal? Decal;

    public RequestDecalRemovalEvent(NetCoordinates coordinates, Decal? decal = null)
    {
        Coordinates = coordinates;
        Decal = decal;
    }
}
