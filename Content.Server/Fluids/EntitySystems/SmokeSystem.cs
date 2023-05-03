using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Chemistry.ReactionEffects;
using Content.Server.Coordinates.Helpers;
using Content.Server.Spreader;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Smoking;
using Content.Shared.Spawners;
using Content.Shared.Spawners.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Fluids.EntitySystems;

/// <summary>
/// Handles non-atmos solution entities similar to puddles.
/// </summary>
public sealed class SmokeSystem : EntitySystem
{
    // If I could do it all again this could probably use a lot more of puddles.
    [Dependency] private readonly IAdminLogManager _logger = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly BloodstreamSystem _blood = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly InternalsSystem _internals = default!;
    [Dependency] private readonly ReactiveSystem _reactive = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SmokeComponent, EntityUnpausedEvent>(OnSmokeUnpaused);
        SubscribeLocalEvent<SmokeComponent, ReactionAttemptEvent>(OnReactionAttempt);
        SubscribeLocalEvent<SmokeComponent, SpreadNeighborsEvent>(OnSmokeSpread);
        SubscribeLocalEvent<SmokeDissipateSpawnComponent, TimedDespawnEvent>(OnSmokeDissipate);
        SubscribeLocalEvent<SpreadGroupUpdateRate>(OnSpreadUpdateRate);
    }

    private void OnSpreadUpdateRate(ref SpreadGroupUpdateRate ev)
    {
        if (ev.Name != "smoke")
            return;

        ev.UpdatesPerSecond = 8;
    }

    private void OnSmokeDissipate(EntityUid uid, SmokeDissipateSpawnComponent component, ref TimedDespawnEvent args)
    {
        if (!TryComp<TransformComponent>(uid, out var xform))
        {
            return;
        }

        Spawn(component.Prototype, xform.Coordinates);
    }

    private void OnSmokeSpread(EntityUid uid, SmokeComponent component, ref SpreadNeighborsEvent args)
    {
        if (component.SpreadAmount == 0 ||
            !_solutionSystem.TryGetSolution(uid, SmokeComponent.SolutionName, out var solution) ||
            args.NeighborFreeTiles.Count == 0)
        {
            RemCompDeferred<EdgeSpreaderComponent>(uid);
            return;
        }

        var prototype = MetaData(uid).EntityPrototype;

        if (prototype == null)
        {
            RemCompDeferred<EdgeSpreaderComponent>(uid);
            return;
        }

        TryComp<TimedDespawnComponent>(uid, out var timer);

        var smokePerSpread = component.SpreadAmount / args.NeighborFreeTiles.Count;
        component.SpreadAmount -= smokePerSpread;

        foreach (var neighbor in args.NeighborFreeTiles)
        {
            var coords = neighbor.Grid.GridTileToLocal(neighbor.Tile);
            var ent = Spawn(prototype.ID, coords.SnapToGrid());
            var neighborSmoke = EnsureComp<SmokeComponent>(ent);
            neighborSmoke.SpreadAmount = Math.Max(0, smokePerSpread - 1);
            args.Updates--;

            // Listen this is the old behaviour iunno
            Start(ent, neighborSmoke, solution.Clone(), timer?.Lifetime ?? 10f);

            if (_appearance.TryGetData(uid, SmokeVisuals.Color, out var color))
            {
                _appearance.SetData(ent, SmokeVisuals.Color, color);
            }

            // Only 1 spread then ig?
            if (smokePerSpread == 0)
            {
                component.SpreadAmount--;

                if (component.SpreadAmount == 0)
                {
                    RemCompDeferred<EdgeSpreaderComponent>(uid);
                    break;
                }
            }

            if (args.Updates <= 0)
                break;
        }

        // Give our spread to neighbor tiles.
        if (args.NeighborFreeTiles.Count == 0 && args.Neighbors.Count > 0 && component.SpreadAmount > 0)
        {
            var smokeQuery = GetEntityQuery<SmokeComponent>();

            foreach (var neighbor in args.Neighbors)
            {
                if (!smokeQuery.TryGetComponent(neighbor, out var smoke))
                    continue;

                smoke.SpreadAmount++;
                args.Updates--;

                if (component.SpreadAmount == 0)
                {
                    RemCompDeferred<EdgeSpreaderComponent>(uid);
                    break;
                }

                if (args.Updates <= 0)
                    break;
            }
        }
    }

    private void OnReactionAttempt(EntityUid uid, SmokeComponent component, ReactionAttemptEvent args)
    {
        if (args.Solution.Name != SmokeComponent.SolutionName)
            return;

        // Prevent smoke/foam fork bombs (smoke creating more smoke).
        foreach (var effect in args.Reaction.Effects)
        {
            if (effect is AreaReactionEffect)
            {
                args.Cancel();
                return;
            }
        }
    }

    private void OnSmokeUnpaused(EntityUid uid, SmokeComponent component, ref EntityUnpausedEvent args)
    {
        component.NextReact += args.PausedTime;
    }

    /// <inheritdoc/>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<SmokeComponent>();
        var curTime = _timing.CurTime;

        while (query.MoveNext(out var uid, out var smoke))
        {
            if (smoke.NextReact > curTime)
                continue;

            smoke.NextReact += TimeSpan.FromSeconds(1.5);

            SmokeReact(uid, 1f, smoke);
        }
    }

    /// <summary>
    /// Does the relevant smoke reactions for an entity for the specified exposure duration.
    /// </summary>
    public void SmokeReact(EntityUid uid, float frameTime, SmokeComponent? component = null, TransformComponent? xform = null)
    {
        if (!Resolve(uid, ref component, ref xform))
            return;

        if (!_solutionSystem.TryGetSolution(uid, SmokeComponent.SolutionName, out var solution) ||
            solution.Contents.Count == 0)
        {
            return;
        }

        if (!_mapManager.TryGetGrid(xform.GridUid, out var mapGrid))
            return;

        var tile = mapGrid.GetTileRef(xform.Coordinates.ToVector2i(EntityManager, _mapManager));

        var solutionFraction = 1 / Math.Floor(frameTime);
        var ents = _lookup.GetEntitiesIntersecting(tile, LookupFlags.Uncontained).ToArray();

        foreach (var reagentQuantity in solution.Contents.ToArray())
        {
            if (reagentQuantity.Quantity == FixedPoint2.Zero)
                continue;

            var reagent = _prototype.Index<ReagentPrototype>(reagentQuantity.ReagentId);

            // React with the tile the effect is on
            // We don't multiply by solutionFraction here since the tile is only ever reacted once
            if (!component.ReactedTile)
            {
                reagent.ReactionTile(tile, reagentQuantity.Quantity);
                component.ReactedTile = true;
            }

            // Touch every entity on tile.
            foreach (var entity in ents)
            {
                if (entity == uid)
                    continue;

                _reactive.ReactionEntity(entity, ReactionMethod.Touch, reagent,
                    reagentQuantity.Quantity * solutionFraction, solution);
            }
        }

        foreach (var entity in ents)
        {
            if (entity == uid)
                continue;

            ReactWithEntity(entity, solution, solutionFraction);
        }

        UpdateVisuals(uid);
    }

    private void UpdateVisuals(EntityUid uid)
    {
        if (TryComp(uid, out AppearanceComponent? appearance) &&
            _solutionSystem.TryGetSolution(uid, SmokeComponent.SolutionName, out var solution))
        {
            var color = solution.GetColor(_prototype);
            _appearance.SetData(uid, SmokeVisuals.Color, color, appearance);
        }
    }

    private void ReactWithEntity(EntityUid entity, Solution solution, double solutionFraction)
    {
        if (!TryComp<BloodstreamComponent>(entity, out var bloodstream))
            return;

        if (TryComp<InternalsComponent>(entity, out var internals) &&
            _internals.AreInternalsWorking(internals))
        {
            return;
        }

        var cloneSolution = solution.Clone();
        var transferAmount = FixedPoint2.Min(cloneSolution.Volume * solutionFraction, bloodstream.ChemicalSolution.AvailableVolume);
        var transferSolution = cloneSolution.SplitSolution(transferAmount);

        foreach (var reagentQuantity in transferSolution.Contents.ToArray())
        {
            if (reagentQuantity.Quantity == FixedPoint2.Zero)
                continue;

            _reactive.ReactionEntity(entity, ReactionMethod.Ingestion, reagentQuantity.ReagentId, reagentQuantity.Quantity, transferSolution);
        }

        if (_blood.TryAddToChemicals(entity, transferSolution, bloodstream))
        {
            // Log solution addition by smoke
            _logger.Add(LogType.ForceFeed, LogImpact.Medium, $"{ToPrettyString(entity):target} was affected by smoke {SolutionContainerSystem.ToPrettyString(transferSolution)}");
        }
    }

    /// <summary>
    /// Sets up a smoke component for spreading.
    /// </summary>
    public void Start(EntityUid uid, SmokeComponent component, Solution solution, float duration)
    {
        TryAddSolution(uid, component, solution);
        EnsureComp<EdgeSpreaderComponent>(uid);
        var timer = EnsureComp<TimedDespawnComponent>(uid);
        timer.Lifetime = duration;
    }

    /// <summary>
    /// Adds the specified solution to the relevant smoke solution.
    /// </summary>
    public void TryAddSolution(EntityUid uid, SmokeComponent component, Solution solution)
    {
        if (solution.Volume == FixedPoint2.Zero)
            return;

        if (!_solutionSystem.TryGetSolution(uid, SmokeComponent.SolutionName, out var solutionArea))
            return;

        var addSolution =
            solution.SplitSolution(FixedPoint2.Min(solution.Volume, solutionArea.AvailableVolume));

        _solutionSystem.TryAddSolution(uid, solutionArea, addSolution);

        UpdateVisuals(uid);
    }
}
