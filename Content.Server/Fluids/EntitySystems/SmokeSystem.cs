using Content.Server.Administration.Logs;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.EntityEffects.Effects;
using Content.Server.Spreader;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Smoking;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Linq;

using TimedDespawnComponent = Robust.Shared.Spawners.TimedDespawnComponent;

namespace Content.Server.Fluids.EntitySystems;

/// <summary>
/// Handles non-atmos solution entities similar to puddles.
/// </summary>
public sealed class SmokeSystem : EntitySystem
{
    // If I could do it all again this could probably use a lot more of puddles.
    [Dependency] private readonly IAdminLogManager _logger = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly BloodstreamSystem _blood = default!;
    [Dependency] private readonly InternalsSystem _internals = default!;
    [Dependency] private readonly ReactiveSystem _reactive = default!;
    [Dependency] private readonly SharedBroadphaseSystem _broadphase = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;

    private EntityQuery<SmokeComponent> _smokeQuery;
    private EntityQuery<SmokeAffectedComponent> _smokeAffectedQuery;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        _smokeQuery = GetEntityQuery<SmokeComponent>();
        _smokeAffectedQuery = GetEntityQuery<SmokeAffectedComponent>();

        SubscribeLocalEvent<SmokeComponent, StartCollideEvent>(OnStartCollide);
        SubscribeLocalEvent<SmokeComponent, EndCollideEvent>(OnEndCollide);
        SubscribeLocalEvent<SmokeComponent, ReactionAttemptEvent>(OnReactionAttempt);
        SubscribeLocalEvent<SmokeComponent, SolutionRelayEvent<ReactionAttemptEvent>>(OnReactionAttempt);
        SubscribeLocalEvent<SmokeComponent, SpreadNeighborsEvent>(OnSmokeSpread);
    }

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

    private void OnStartCollide(Entity<SmokeComponent> entity, ref StartCollideEvent args)
    {
        if (_smokeAffectedQuery.HasComponent(args.OtherEntity))
            return;

        var smokeAffected = AddComp<SmokeAffectedComponent>(args.OtherEntity);
        smokeAffected.SmokeEntity = entity;
        smokeAffected.NextSecond = _timing.CurTime + TimeSpan.FromSeconds(1);
    }

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

    private void OnSmokeSpread(Entity<SmokeComponent> entity, ref SpreadNeighborsEvent args)
    {
        if (entity.Comp.SpreadAmount == 0 || !_solutionContainerSystem.ResolveSolution(entity.Owner, SmokeComponent.SolutionName, ref entity.Comp.Solution, out var solution))
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
            var ent = Spawn(prototype.ID, coords);
            var spreadAmount = Math.Max(0, smokePerSpread);
            entity.Comp.SpreadAmount -= args.NeighborFreeTiles.Count;

            StartSmoke(ent, solution.Clone(), timer?.Lifetime ?? entity.Comp.Duration, spreadAmount);

            if (entity.Comp.SpreadAmount == 0)
            {
                RemCompDeferred<ActiveEdgeSpreaderComponent>(entity);
                break;
            }
        }

        args.Updates--;

        if (args.NeighborFreeTiles.Count > 0 || args.Neighbors.Count == 0 || entity.Comp.SpreadAmount < 1)
            return;

        // We have no more neighbours to spread to. So instead we will randomly distribute our volume to neighbouring smoke tiles.

        var smokeQuery = GetEntityQuery<SmokeComponent>();

        _random.Shuffle(args.Neighbors);
        foreach (var neighbor in args.Neighbors)
        {
            if (!smokeQuery.TryGetComponent(neighbor, out var smoke))
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

    private void OnReactionAttempt(Entity<SmokeComponent> entity, ref SolutionRelayEvent<ReactionAttemptEvent> args)
    {
        if (args.Name == SmokeComponent.SolutionName)
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
        component.TransferRate = solution.Volume / duration;
        TryAddSolution(uid, solution);
        Dirty(uid, component);
        EnsureComp<ActiveEdgeSpreaderComponent>(uid);

        if (TryComp<PhysicsComponent>(uid, out var body) && TryComp<FixturesComponent>(uid, out var fixtures))
        {
            var xform = Transform(uid);
            _physics.SetBodyType(uid, BodyType.Dynamic, fixtures, body, xform);
            _physics.SetCanCollide(uid, true, manager: fixtures, body: body);
            _broadphase.RegenerateContacts((uid, body, fixtures, xform));
        }

        var timer = EnsureComp<TimedDespawnComponent>(uid);
        timer.Lifetime = duration;

        // The tile reaction happens here because it only occurs once.
        ReactOnTile(uid, component);
    }

    /// <summary>
    /// Does the relevant smoke reactions for an entity.
    /// </summary>
    public void SmokeReact(EntityUid entity, EntityUid smokeUid, SmokeComponent? component = null)
    {
        if (!Resolve(smokeUid, ref component))
            return;

        if (!_solutionContainerSystem.ResolveSolution(smokeUid, SmokeComponent.SolutionName, ref component.Solution, out var solution) ||
            solution.Contents.Count == 0)
        {
            return;
        }

        ReactWithEntity(entity, smokeUid, solution, component);
        UpdateVisuals((smokeUid, component));
    }

    private void ReactWithEntity(EntityUid entity, EntityUid smokeUid, Solution solution, SmokeComponent? component = null)
    {
        if (!Resolve(smokeUid, ref component))
            return;

        if (!TryComp<BloodstreamComponent>(entity, out var bloodstream))
            return;

        if (!_solutionContainerSystem.ResolveSolution(entity, bloodstream.ChemicalSolutionName, ref bloodstream.ChemicalSolution, out var chemSolution) || chemSolution.AvailableVolume <= 0)
            return;

        var blockIngestion = _internals.AreInternalsWorking(entity);

        var cloneSolution = solution.Clone();
        var availableTransfer = FixedPoint2.Min(cloneSolution.Volume, component.TransferRate);
        var transferAmount = FixedPoint2.Min(availableTransfer, chemSolution.AvailableVolume);
        var transferSolution = cloneSolution.SplitSolution(transferAmount);

        foreach (var reagentQuantity in transferSolution.Contents.ToArray())
        {
            if (reagentQuantity.Quantity == FixedPoint2.Zero)
                continue;
            var reagentProto = _prototype.Index<ReagentPrototype>(reagentQuantity.Reagent.Prototype);

            _reactive.ReactionEntity(entity, ReactionMethod.Touch, reagentProto, reagentQuantity, transferSolution);
            if (!blockIngestion)
                _reactive.ReactionEntity(entity, ReactionMethod.Ingestion, reagentProto, reagentQuantity, transferSolution);
        }

        if (blockIngestion)
            return;

        if (_blood.TryAddToChemicals(entity, transferSolution, bloodstream))
        {
            // Log solution addition by smoke
            _logger.Add(LogType.ForceFeed, LogImpact.Medium, $"{ToPrettyString(entity):target} ingested smoke {SharedSolutionContainerSystem.ToPrettyString(transferSolution)}");
        }
    }

    private void ReactOnTile(EntityUid uid, SmokeComponent? component = null, TransformComponent? xform = null)
    {
        if (!Resolve(uid, ref component, ref xform))
            return;

        if (!_solutionContainerSystem.ResolveSolution(uid, SmokeComponent.SolutionName, ref component.Solution, out var solution) || !solution.Any())
            return;

        if (!TryComp<MapGridComponent>(xform.GridUid, out var mapGrid))
            return;

        var tile = _map.GetTileRef(xform.GridUid.Value, mapGrid, xform.Coordinates);

        foreach (var reagentQuantity in solution.Contents.ToArray())
        {
            if (reagentQuantity.Quantity == FixedPoint2.Zero)
                continue;

            var reagent = _prototype.Index<ReagentPrototype>(reagentQuantity.Reagent.Prototype);
            reagent.ReactionTile(tile, reagentQuantity.Quantity, EntityManager, reagentQuantity.Reagent.Data);
        }
    }

    /// <summary>
    /// Adds the specified solution to the relevant smoke solution.
    /// </summary>
    private void TryAddSolution(Entity<SmokeComponent?> smoke, Solution solution)
    {
        if (solution.Volume == FixedPoint2.Zero)
            return;

        if (!Resolve(smoke, ref smoke.Comp))
            return;

        if (!_solutionContainerSystem.ResolveSolution(smoke.Owner, SmokeComponent.SolutionName, ref smoke.Comp.Solution, out var solutionArea))
            return;

        var addSolution = solution.SplitSolution(FixedPoint2.Min(solution.Volume, solutionArea.AvailableVolume));
        _solutionContainerSystem.TryAddSolution(smoke.Comp.Solution.Value, addSolution);

        UpdateVisuals(smoke);
    }

    private void UpdateVisuals(Entity<SmokeComponent?, AppearanceComponent?> smoke)
    {
        if (!Resolve(smoke, ref smoke.Comp1, ref smoke.Comp2) ||
            !_solutionContainerSystem.ResolveSolution(smoke.Owner, SmokeComponent.SolutionName, ref smoke.Comp1.Solution, out var solution))
            return;

        var color = solution.GetColor(_prototype);
        _appearance.SetData(smoke.Owner, SmokeVisuals.Color, color, smoke.Comp2);
    }
}
