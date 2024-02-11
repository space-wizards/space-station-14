using System.Linq;
using System.Numerics;
using Content.Server.Worldgen.Components;
using Content.Server.Worldgen.Components.Debris;
using Content.Server.Worldgen.Tools;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Worldgen.Systems.Debris;

/// <summary>
///     This handles placing debris within the world evenly with rng, primarily for structures like asteroid fields.
/// </summary>
public sealed class DebrisFeaturePlacerSystem : BaseWorldSystem
{
    [Dependency] private readonly NoiseIndexSystem _noiseIndex = default!;
    [Dependency] private readonly PoissonDiskSampler _sampler = default!;
    [Dependency] private readonly TransformSystem _xformSys = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private ISawmill _sawmill = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        _sawmill = _logManager.GetSawmill("world.debris.feature_placer");
        SubscribeLocalEvent<DebrisFeaturePlacerControllerComponent, WorldChunkLoadedEvent>(OnChunkLoaded);
        SubscribeLocalEvent<DebrisFeaturePlacerControllerComponent, WorldChunkUnloadedEvent>(OnChunkUnloaded);
        SubscribeLocalEvent<OwnedDebrisComponent, ComponentShutdown>(OnDebrisShutdown);
        SubscribeLocalEvent<OwnedDebrisComponent, MoveEvent>(OnDebrisMove);
        SubscribeLocalEvent<SimpleDebrisSelectorComponent, TryGetPlaceableDebrisFeatureEvent>(
            OnTryGetPlacableDebrisEvent);
    }

    /// <summary>
    ///     Handles debris moving, and making sure it stays parented to a chunk for loading purposes.
    /// </summary>
    private void OnDebrisMove(EntityUid uid, OwnedDebrisComponent component, ref MoveEvent args)
    {
        if (!HasComp<WorldChunkComponent>(component.OwningController))
            return; // Redundant logic, prolly needs it's own handler for your custom system.

        var placer = Comp<DebrisFeaturePlacerControllerComponent>(component.OwningController);
        var xform = args.Component;
        var ownerXform = Transform(component.OwningController);
        if (xform.MapUid is null || ownerXform.MapUid is null)
            return; // not our problem

        if (xform.MapUid != ownerXform.MapUid)
        {
            _sawmill.Error($"Somehow debris {uid} left it's expected map! Unparenting it to avoid issues.");
            RemCompDeferred<OwnedDebrisComponent>(uid);
            placer.OwnedDebris.Remove(component.LastKey);
            return;
        }

        placer.OwnedDebris.Remove(component.LastKey);
        var newChunk = GetOrCreateChunk(GetChunkCoords(uid), xform.MapUid!.Value);
        if (newChunk is null || !TryComp<DebrisFeaturePlacerControllerComponent>(newChunk, out var newPlacer))
        {
            // Whelp.
            RemCompDeferred<OwnedDebrisComponent>(uid);
            return;
        }

        newPlacer.OwnedDebris[_xformSys.GetWorldPosition(xform)] = uid; // Change our owner.
        component.OwningController = newChunk.Value;
    }

    /// <summary>
    ///     Handles debris shutdown/detach.
    /// </summary>
    private void OnDebrisShutdown(EntityUid uid, OwnedDebrisComponent component, ComponentShutdown args)
    {
        if (!TryComp<DebrisFeaturePlacerControllerComponent>(component.OwningController, out var placer))
            return;

        placer.OwnedDebris[component.LastKey] = null;
        if (Terminating(uid))
            placer.OwnedDebris.Remove(component.LastKey);
    }

    /// <summary>
    ///     Queues all debris owned by the placer for garbage collection.
    /// </summary>
    private void OnChunkUnloaded(EntityUid uid, DebrisFeaturePlacerControllerComponent component,
        ref WorldChunkUnloadedEvent args)
    {
        component.DoSpawns = true;
    }

    /// <summary>
    ///     Handles providing a debris type to place for SimpleDebrisSelectorComponent.
    ///     This randomly picks a debris type from the EntitySpawnCollectionCache.
    /// </summary>
    private void OnTryGetPlacableDebrisEvent(EntityUid uid, SimpleDebrisSelectorComponent component,
        ref TryGetPlaceableDebrisFeatureEvent args)
    {
        if (args.DebrisProto is not null)
            return;

        var l = new List<string?>(1);
        component.CachedDebrisTable.GetSpawns(_random, ref l);

        switch (l.Count)
        {
            case 0:
                return;
            case > 1:
                _sawmill.Warning($"Got more than one possible debris type from {uid}. List: {string.Join(", ", l)}");
                break;
        }

        args.DebrisProto = l[0];
    }

    /// <summary>
    ///     Handles loading in debris. This does the following:
    ///     - Checks if the debris is currently supposed to do spawns, if it isn't, aborts immediately.
    ///     - Evaluates the density value to be used for placement, if it's zero, aborts.
    ///     - Generates the points to generate debris at, if and only if they've not been selected already by a prior load.
    ///     - Does the following in a loop over all generated points:
    ///         - Raises an event to check if something else wants to intercept debris placement, if the event is handled,
    ///           continues to the next point without generating anything.
    ///         - Raises an event to get the debris type that should be used for generation.
    ///         - Spawns the given debris at the point, adding it to the placer's index.
    /// </summary>
    private void OnChunkLoaded(EntityUid uid, DebrisFeaturePlacerControllerComponent component,
        ref WorldChunkLoadedEvent args)
    {
        if (component.DoSpawns == false)
            return;

        component.DoSpawns = false; // Don't repeat yourself if this crashes.

        var chunk = Comp<WorldChunkComponent>(args.Chunk);
        var densityChannel = component.DensityNoiseChannel;
        var density = _noiseIndex.Evaluate(uid, densityChannel, chunk.Coordinates + new Vector2(0.5f, 0.5f));
        if (density == 0)
            return;

        List<Vector2>? points = null;

        // If we've been loaded before, reuse the same coordinates.
        if (component.OwnedDebris.Count != 0)
        {
            //TODO: Remove LINQ.
            points = component.OwnedDebris
                .Where(x => !Deleted(x.Value))
                .Select(static x => x.Key)
                .ToList();
        }

        points ??= GeneratePointsInChunk(args.Chunk, density, chunk.Coordinates, chunk.Map);

        var safetyBounds = Box2.UnitCentered.Enlarged(component.SafetyZoneRadius);
        var failures = 0; // Avoid severe log spam.
        foreach (var point in points)
        {
            if (component.OwnedDebris.TryGetValue(point, out var existing))
            {
                DebugTools.Assert(Exists(existing));
                continue;
            }

            var pointDensity = _noiseIndex.Evaluate(uid, densityChannel, WorldGen.WorldToChunkCoords(point));
            if (pointDensity == 0 && component.DensityClip || _random.Prob(component.RandomCancellationChance))
                continue;

            var coords = new EntityCoordinates(chunk.Map, point);

            if (_mapManager
                .FindGridsIntersecting(Comp<MapComponent>(chunk.Map).MapId, safetyBounds.Translated(point)).Any())
                continue; // Oops, gonna collide.

            var preEv = new PrePlaceDebrisFeatureEvent(coords, args.Chunk);
            RaiseLocalEvent(uid, ref preEv);
            if (uid != args.Chunk)
                RaiseLocalEvent(args.Chunk, ref preEv);

            if (preEv.Handled)
                continue;

            var debrisFeatureEv = new TryGetPlaceableDebrisFeatureEvent(coords, args.Chunk);
            RaiseLocalEvent(uid, ref debrisFeatureEv);

            if (debrisFeatureEv.DebrisProto == null)
            {
                // Try on the chunk...?
                if (uid != args.Chunk)
                    RaiseLocalEvent(args.Chunk, ref debrisFeatureEv);

                if (debrisFeatureEv.DebrisProto == null)
                {
                    // Nope.
                    failures++;
                    continue;
                }
            }

            var ent = Spawn(debrisFeatureEv.DebrisProto, coords);
            component.OwnedDebris.Add(point, ent);

            var owned = EnsureComp<OwnedDebrisComponent>(ent);
            owned.OwningController = uid;
            owned.LastKey = point;
        }

        if (failures > 0)
            _sawmill.Error($"Failed to place {failures} debris at chunk {args.Chunk}");
    }

    /// <summary>
    ///     Generates the points to put into a chunk using a poisson disk sampler.
    /// </summary>
    private List<Vector2> GeneratePointsInChunk(EntityUid chunk, float density, Vector2 coords, EntityUid map)
    {
        var offs = (int) ((WorldGen.ChunkSize - WorldGen.ChunkSize / 8.0f) / 2.0f);
        var topLeft = new Vector2(-offs, -offs);
        var lowerRight = new Vector2(offs, offs);
        var enumerator = _sampler.SampleRectangle(topLeft, lowerRight, density);
        var debrisPoints = new List<Vector2>();

        var realCenter = WorldGen.ChunkToWorldCoordsCentered(coords.Floored());

        while (enumerator.MoveNext(out var debrisPoint))
        {
            debrisPoints.Add(realCenter + debrisPoint.Value);
        }

        return debrisPoints;
    }
}

/// <summary>
///     Fired directed on the debris feature placer controller and the chunk, ahead of placing a debris piece.
/// </summary>
[ByRefEvent]
[PublicAPI]
public record struct PrePlaceDebrisFeatureEvent(EntityCoordinates Coords, EntityUid Chunk, bool Handled = false);

/// <summary>
///     Fired directed on the debris feature placer controller and the chunk, to select which debris piece to place.
/// </summary>
[ByRefEvent]
[PublicAPI]
public record struct TryGetPlaceableDebrisFeatureEvent(EntityCoordinates Coords, EntityUid Chunk,
    string? DebrisProto = null);

