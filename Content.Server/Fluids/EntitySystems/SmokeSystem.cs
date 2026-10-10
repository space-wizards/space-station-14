using System.Diagnostics.CodeAnalysis;
using Content.Server.Administration.Logs;
using Content.Server.Body.Systems;
using Content.Server.Spreader;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Smoking;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Linq;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.EntityEffects.Effects.Solution;
using Robust.Shared.Map;
using TimedDespawnComponent = Robust.Shared.Spawners.TimedDespawnComponent;

namespace Content.Server.Fluids.EntitySystems;

/// <summary>
/// Handles non-atmos solution entities similar to puddles.
/// </summary>
public sealed partial class SmokeSystem : EntitySystem
{
    // If I could do it all again this could probably use a lot more of puddles.
    [Dependency] private IAdminLogManager _logger = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private BloodstreamSystem _blood = default!;
    [Dependency] private InternalsSystem _internals = default!;
    [Dependency] private ReactiveSystem _reactive = default!;
    [Dependency] private SharedBroadphaseSystem _broadphase = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainerSystem = default!;

    [Dependency] private EntityQuery<SmokeComponent> _smokeQuery = default!;
    [Dependency] private EntityQuery<SmokeAffectedComponent> _smokeAffectedQuery = default!;

    /// <inheritdoc/>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SmokeAffectedComponent>();
        var curTime = _timing.CurTime;
        while (query.MoveNext(out var uid, out var smoke))
        {
            if (curTime < smoke.NextSecond)
                continue;

            smoke.NextSecond += TimeSpan.FromSeconds(1);
            SmokeReact(uid, smoke.SmokeEntity);
        }
    }

    [SubscribeLocalEvent]
    private void OnComponentInit(Entity<SmokeComponent> entity, ref ComponentInit args)
    {
        if (entity.Comp.StartingContents == null)
            return;

        StartSmoke(entity, entity.Comp.StartingContents, entity.Comp.Duration, entity.Comp.SpreadAmount, entity.Comp);
    }

    [SubscribeLocalEvent]
    private void OnStartCollide(Entity<SmokeComponent> entity, ref StartCollideEvent args)
    {
        if (_smokeAffectedQuery.HasComponent(args.OtherEntity))
            return;

        var smokeAffected = AddComp<SmokeAffectedComponent>(args.OtherEntity);
        smokeAffected.SmokeEntity = entity;
        smokeAffected.NextSecond = _timing.CurTime + TimeSpan.FromSeconds(1);
    }

    [SubscribeLocalEvent]
    private void OnEndCollide(Entity<SmokeComponent> entity, ref EndCollideEvent args)
    {
        // if we are already in smoke, make sure the thing we are exiting is the current smoke we are in.
        if (_smokeAffectedQuery.TryGetComponent(args.OtherEntity, out var smokeAffectedComponent))
        {
            if (smokeAffectedComponent.SmokeEntity != entity.Owner)
                return;
        }

        var exists = Exists(entity);

        if (!TryComp<PhysicsComponent>(args.OtherEntity, out var body))
            return;

        foreach (var ent in _physics.GetContactingEntities(args.OtherEntity, body))
        {
            if (exists && ent == entity.Owner)
                continue;

            if (!_smokeQuery.HasComponent(ent))
                continue;

            smokeAffectedComponent ??= EnsureComp<SmokeAffectedComponent>(args.OtherEntity);
            smokeAffectedComponent.SmokeEntity = ent;
            return; // exit the function so we don't remove the component.
        }

        if (smokeAffectedComponent != null)
            RemComp(args.OtherEntity, smokeAffectedComponent);
    }

    [SubscribeLocalEvent]
    private void OnSmokeSpread(Entity<SmokeComponent> entity, ref SpreadNeighborsEvent args)
    {
        if (entity.Comp.SpreadAmount == 0)
        {
            RemCompDeferred<ActiveEdgeSpreaderComponent>(entity);
            return;
        }

        if (Prototype(entity) is not { } prototype)
        {
            RemCompDeferred<ActiveEdgeSpreaderComponent>(entity);
            return;
        }

        if (args.NeighborFreeTiles.Count == 0)
            return;

        TryComp<TimedDespawnComponent>(entity, out var timer);

        // wtf is the logic behind any of this.
        var smokePerSpread = entity.Comp.SpreadAmount / Math.Max(1, args.NeighborFreeTiles.Count);
        foreach (var neighbor in args.NeighborFreeTiles)
        {
            var coords = _map.GridTileToLocal(neighbor.Tile.GridUid, neighbor.Grid, neighbor.Tile.GridIndices);
            var ent = EntityManager.CreateEntityUninitialized(prototype.ID, coords);
            // If the smoke entity has starting contents, new entities spawned from it should not include it.
            if (entity.Comp.StartingContents != null && TryComp<SmokeComponent>(ent, out var newSmoke))
            {
                newSmoke.StartingContents = null;
            }
            EntityManager.InitializeAndStartEntity(ent);

            var spreadAmount = Math.Max(0, smokePerSpread);
            entity.Comp.SpreadAmount -= args.NeighborFreeTiles.Count;

            SpreadSmoke(ent, entity.AsNullable(), spreadAmount, timer?.Lifetime ?? entity.Comp.Duration);

            if (entity.Comp.SpreadAmount == 0)
            {
                RemCompDeferred<ActiveEdgeSpreaderComponent>(entity);
                break;
            }
        }

        args.Updates--;


        if (entity.Comp.SmokeSourceEntity != null)
        {
            var smokeSrcComp = entity.Comp.SmokeSourceEntity.Value.Comp;

            // If smoke has spread, we need to re-calculate the transfer rate.
            if (smokeSrcComp.DirtyTransferRateCalc)
                smokeSrcComp.TransferRate = CalculateTransferRate(smokeSrcComp.OriginalVolume, smokeSrcComp.SpreadCount, smokeSrcComp.Duration);
        }

        if (args.NeighborFreeTiles.Count > 0 || args.Neighbors.Count == 0 || entity.Comp.SpreadAmount < 1)
            return;

        // We have no more neighbours to spread to. So instead we will randomly distribute our volume to neighbouring smoke tiles.

        _random.Shuffle(args.Neighbors);
        foreach (var neighbor in args.Neighbors)
        {
            if (!_smokeQuery.TryGetComponent(neighbor, out var smoke))
                continue;

            smoke.SpreadAmount++;
            entity.Comp.SpreadAmount--;
            EnsureComp<ActiveEdgeSpreaderComponent>(neighbor);

            if (entity.Comp.SpreadAmount == 0)
            {
                RemCompDeferred<ActiveEdgeSpreaderComponent>(entity);
                break;
            }
        }

    }

    [SubscribeLocalEvent]
    private void OnReactionAttempt(Entity<SmokeComponent> entity, ref ReactionAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        // Prevent smoke/foam fork bombs (smoke creating more smoke).
        foreach (var effect in args.Reaction.Effects)
        {
            if (effect is AreaReactionEffect)
            {
                args.Cancelled = true;
                return;
            }
        }
    }

    [SubscribeLocalEvent]
    private void OnReactionAttempt(Entity<SmokeComponent> entity, ref SolutionRelayEvent<ReactionAttemptEvent> args)
    {
        if (args.Solution.Comp.Id == SmokeComponent.SolutionName)
            OnReactionAttempt(entity, ref args.Event);
    }

    /// <summary>
    /// Sets up a smoke component for spreading.
    /// </summary>
    public void StartSmoke(EntityUid uid, Solution solution, float duration, int spreadAmount, SmokeComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.SpreadAmount = spreadAmount;
        component.Duration = duration;

        SetUpSmokePhysics(uid, duration);
        Dirty(uid, component);

        if (!TryAddSolution(uid, solution, out var smokeSrcEnt, out var volume))
            return;

        var smokeSrcComp = smokeSrcEnt.Value.Comp;

        smokeSrcComp.SpreadCount++;
        smokeSrcComp.OriginalVolume = volume;
        smokeSrcComp.Duration = duration;
        smokeSrcComp.TransferRate =  CalculateTransferRate(smokeSrcComp.OriginalVolume, smokeSrcComp.SpreadCount, smokeSrcComp.Duration);

        var solutionTimer = EnsureComp<TimedDespawnComponent>(smokeSrcEnt.Value);
        solutionTimer.Lifetime = duration;

        // The tile reaction happens here because it only occurs once.
        ReactOnTile(uid, component);
    }

    private void SpreadSmoke(Entity<SmokeComponent?> newEntity, Entity<SmokeComponent?> sourceEntity, int spreadAmount, float duration)
    {
        if (!Resolve(newEntity, ref newEntity.Comp))
            return;

        if (!Resolve(sourceEntity, ref sourceEntity.Comp))
            return;

        newEntity.Comp.SpreadAmount = spreadAmount;
        newEntity.Comp.Duration = duration;
        newEntity.Comp.SmokeSourceEntity = sourceEntity.Comp.SmokeSourceEntity;

        SetUpSmokePhysics(newEntity, duration);

        UpdateVisuals(newEntity);
        Dirty(newEntity);

        // Handle everything related to contained reagents

        if (sourceEntity.Comp.SmokeSourceEntity == null)
            return;

        var smokeSrcComp = sourceEntity.Comp.SmokeSourceEntity.Value.Comp;
        smokeSrcComp.SpreadCount++;
        smokeSrcComp.DirtyTransferRateCalc = true;

        var solutionTimer = EnsureComp<TimedDespawnComponent>(sourceEntity.Comp.SmokeSourceEntity.Value);
        if (solutionTimer.Lifetime < duration)
            solutionTimer.Lifetime = duration;

        // The tile reaction happens here because it only occurs once.
        ReactOnTile(newEntity, newEntity.Comp);
    }

    private void SetUpSmokePhysics(EntityUid entity, float duration)
    {
        EnsureComp<ActiveEdgeSpreaderComponent>(entity);

        if (TryComp<PhysicsComponent>(entity, out var body) && TryComp<FixturesComponent>(entity, out var fixtures))
        {
            var xform = Transform(entity);
            _physics.SetBodyType(entity, BodyType.Dynamic, fixtures, body, xform);
            _physics.SetCanCollide(entity, true, manager: fixtures, body: body);
            _broadphase.RegenerateContacts((entity, body, fixtures, xform));
        }

        var timer = EnsureComp<TimedDespawnComponent>(entity);
        timer.Lifetime = duration;
    }

    /// <summary>
    /// Smoke spreads outwards in what's known as a "centered square number" pattern.
    /// The transfer rate is the reverse of that formula, so that smoke that spreads out in a 3 tile radius returns vol / 3 * duration.
    /// This should give a somewhat correct correlation where a bigger cloud = diluted more.
    /// </summary>
    private FixedPoint2 CalculateTransferRate(FixedPoint2 originalVolume, int spreadCount, float duration)
    {
        if (duration == 0)
            return FixedPoint2.Zero;

        return originalVolume / (((1 + MathF.Sqrt(2 * spreadCount - 1)) / 2) * duration);
    }

    /// <summary>
    /// Does the relevant smoke reactions for an entity.
    /// </summary>
    public void SmokeReact(EntityUid entity, EntityUid smokeUid, SmokeComponent? component = null)
    {
        if (!Resolve(smokeUid, ref component))
            return;

        if (component.SmokeSourceEntity == null ||
            !_solutionContainerSystem.TryGetSolution((component.SmokeSourceEntity.Value, null), SmokeComponent.SolutionName, out _, out var solution) ||
            solution.Contents.Count == 0)
            return;

        ReactWithEntity(entity, smokeUid, solution, component);
        UpdateVisuals((smokeUid, component));
    }

    private void ReactWithEntity(EntityUid entity, EntityUid smokeUid, Solution solution, SmokeComponent? component = null)
    {
        if (!Resolve(smokeUid, ref component))
            return;

        if (component.SmokeSourceEntity == null)
            return;

        if (!TryComp<BloodstreamComponent>(entity, out var bloodstream))
            return;

        if (!_solutionContainerSystem.ResolveSolution(entity, bloodstream.BloodSolutionName, ref bloodstream.BloodSolution, out var bloodSolution) || bloodSolution.AvailableVolume <= 0)
            return;

        var blockIngestion = _internals.AreInternalsWorking(entity);

        var availableTransfer = FixedPoint2.Min(solution.Volume, component.SmokeSourceEntity.Value.Comp.TransferRate);
        var transferAmount = FixedPoint2.Min(availableTransfer, bloodSolution.AvailableVolume);
        var transferSolution = solution.SplitSolution(transferAmount);

        foreach (var reagentQuantity in transferSolution.Contents.ToArray())
        {
            if (reagentQuantity.Quantity == FixedPoint2.Zero)
                continue;

            _reactive.ReactionEntity(entity, ReactionMethod.Touch, reagentQuantity);
            if (!blockIngestion)
                _reactive.ReactionEntity(entity, ReactionMethod.Ingestion, reagentQuantity);
        }

        if (blockIngestion)
            return;

        if (_blood.TryAddToBloodstream((entity, bloodstream), transferSolution))
        {
            // Log solution addition by smoke
            _logger.Add(LogType.ForceFeed, LogImpact.Medium, $"{ToPrettyString(entity):target} ingested smoke {SharedSolutionContainerSystem.ToPrettyString(transferSolution)}");
        }
    }

    private void ReactOnTile(EntityUid uid, SmokeComponent? component = null, TransformComponent? xform = null)
    {
        if (!Resolve(uid, ref component, ref xform))
            return;

        if (component.SmokeSourceEntity == null ||
            !_solutionContainerSystem.TryGetSolution((component.SmokeSourceEntity.Value, null), SmokeComponent.SolutionName, out _, out var solution) || !solution.Any())
            return;

        if (!TryComp<MapGridComponent>(xform.GridUid, out var mapGrid))
            return;

        var tile = _map.GetTileRef(xform.GridUid.Value, mapGrid, xform.Coordinates);

        foreach (var reagentQuantity in solution.Contents.ToArray())
        {
            if (reagentQuantity.Quantity == FixedPoint2.Zero)
                continue;

            var reagent = ProtoMan.Index(reagentQuantity.Reagent.Prototype);
            reagent.ReactionTile(tile, reagentQuantity.Quantity, EntityManager, reagentQuantity.Reagent.Data);
        }
    }

    /// <summary>
    /// Adds the specified solution to the relevant smoke solution.
    /// </summary>
    private bool TryAddSolution(Entity<SmokeComponent?> smoke, Solution solution, [NotNullWhen(true)] out Entity<SmokeSourceComponent>? smokeSource, out FixedPoint2 volume)
    {
        smokeSource = null;
        volume = 0f;

        if (!Resolve(smoke, ref smoke.Comp))
            return false;

        if (smoke.Comp.SmokeSourceEntity != null) // SolutionManager entity already exists?
        {
            Log.Error($"Attempted to create a new smoke source entity for a smoke entity that already has one? Smoke entity: {smoke.ToString()}; Existing smoke source entity: {smoke.Comp.SmokeSourceEntity.Value.ToString()}");
            return false;
        }

        var smokeSrcEnt = Spawn(null, MapCoordinates.Nullspace);
        var solManComp = EnsureComp<SolutionManagerComponent>(smokeSrcEnt);
        var smokeSrcComp = EnsureComp<SmokeSourceComponent>(smokeSrcEnt);
        _solutionContainerSystem.EnsureSolution((smokeSrcEnt, solManComp), SmokeComponent.SolutionName, out var solEnt);
        _solutionContainerSystem.SetCapacity(solEnt, smokeSrcComp.MaxVolume);

        // If the solution is empty (i.e. pure foam) we skip this, but we let the foam spread still.
        if (solution.Volume != FixedPoint2.Zero)
        {
            var addSolution =
                solution.SplitSolution(FixedPoint2.Min(solution.Volume, solEnt.Comp.Solution.AvailableVolume));
            _solutionContainerSystem.TryAddSolution(solEnt, addSolution);
        }

        smokeSrcComp.SmokeColor = solEnt.Comp.Solution.Volume == 0 ? Color.White : solEnt.Comp.Solution.GetColor(ProtoMan);

        smoke.Comp.SmokeSourceEntity = (smokeSrcEnt, smokeSrcComp);

        volume = solEnt.Comp.Solution.Volume;
        smokeSource = smoke.Comp.SmokeSourceEntity;

        UpdateVisuals(smoke);
        return true;
    }

    private void UpdateVisuals(Entity<SmokeComponent?, AppearanceComponent?> smoke)
    {
        if (!Resolve(smoke, ref smoke.Comp1, ref smoke.Comp2) || smoke.Comp1.SmokeSourceEntity == null)
            return;

        _appearance.SetData(smoke.Owner, SmokeVisuals.Color, smoke.Comp1.SmokeSourceEntity.Value.Comp.SmokeColor, smoke.Comp2);
    }
}
