using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Clothing.Components;
using Content.Server.Coordinates.Helpers;
using Content.Server.Fluids.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory.Events;
using Content.Shared.Throwing;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.Fluids.EntitySystems;

[UsedImplicitly]
public class SpillableSystem : EntitySystem
{
    [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly PuddleSystem _puddleSystem = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityLookup _entityLookup = default!;
    [Dependency] private readonly AdminLogSystem _logSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpillableComponent, LandEvent>(SpillOnLand);
        SubscribeLocalEvent<SpillableComponent, GetOtherVerbsEvent>(AddSpillVerb);
        SubscribeLocalEvent<SpillableComponent, GotEquippedEvent>(OnGotEquipped);
    }

    private void OnGotEquipped(EntityUid uid, SpillableComponent component, GotEquippedEvent args)
    {
        if (!component.SpillWorn)
            return;

        if (!TryComp(uid, out ClothingComponent? clothing))
            return;

        // check if entity was actually used as clothing
        // not just taken in pockets or something
        var isCorrectSlot = clothing.SlotFlags.HasFlag(args.SlotFlags);
        if (!isCorrectSlot) return;

        if (!_solutionContainerSystem.TryGetSolution(uid, component.SolutionName, out var solution))
            return;
        if (solution.TotalVolume == 0)
            return;

        // spill all solution on the player
        var drainedSolution = _solutionContainerSystem.Drain(uid, solution, solution.DrainAvailable);
        SpillAt(args.Equipee, drainedSolution, "PuddleSmear");

    }

    /// <summary>
    ///     Spills the specified solution at the entity's location if possible.
    /// </summary>
    /// <param name="uid">
    ///     The entity to use as a location to spill the solution at.
    /// </param>
    /// <param name="solution">Initial solution for the prototype.</param>
    /// <param name="prototype">The prototype to use.</param>
    /// <param name="sound">Play the spill sound.</param>
    /// <param name="combine">Whether to attempt to merge with existing puddles</param>
    /// <param name="transformComponent">Optional Transform component</param>
    /// <returns>The puddle if one was created, null otherwise.</returns>
    public PuddleComponent? SpillAt(EntityUid uid, Solution solution, string prototype,
        bool sound = true, bool combine = true, TransformComponent? transformComponent = null)
    {
        return !Resolve(uid, ref transformComponent, false)
            ? null
            : SpillAt(solution, transformComponent.Coordinates, prototype, sound: sound, combine: combine);
    }

    private void SpillOnLand(EntityUid uid, SpillableComponent component, LandEvent args)
    {
        if (!_solutionContainerSystem.TryGetSolution(uid, component.SolutionName, out var solution)) return;

        if (args.User != null)
        {
            _logSystem.Add(LogType.Landed,
                $"{ToPrettyString(uid):entity} spilled a solution {SolutionContainerSystem.ToPrettyString(solution):solution} on landing");
        }

        var drainedSolution = _solutionContainerSystem.Drain(uid, solution, solution.DrainAvailable);
        SpillAt(drainedSolution, EntityManager.GetComponent<TransformComponent>(uid).Coordinates, "PuddleSmear");
    }

    private void AddSpillVerb(EntityUid uid, SpillableComponent component, GetOtherVerbsEvent args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!_solutionContainerSystem.TryGetDrainableSolution(args.Target, out var solution))
            return;

        if (solution.DrainAvailable == FixedPoint2.Zero)
            return;

        Verb verb = new();
        verb.Text = Loc.GetString("spill-target-verb-get-data-text");
        // TODO VERB ICONS spill icon? pouring out a glass/beaker?
        verb.Act = () =>
        {
            var puddleSolution = _solutionContainerSystem.SplitSolution(args.Target,
                solution, solution.DrainAvailable);
            SpillAt(puddleSolution, Transform(args.Target).Coordinates, "PuddleSmear");
        };
        verb.Impact = LogImpact.Medium; // dangerous reagent reaction are logged separately.
        args.Verbs.Add(verb);
    }

    /// <summary>
    ///     Spills solution at the specified grid coordinates.
    /// </summary>
    /// <param name="solution">Initial solution for the prototype.</param>
    /// <param name="coordinates">The coordinates to spill the solution at.</param>
    /// <param name="prototype">The prototype to use.</param>
    /// <param name="overflow">If the puddle overflow will be calculated. Defaults to true.</param>
    /// <param name="sound">Whether or not to play the spill sound.</param>
    /// <param name="combine">Whether to attempt to merge with existing puddles</param>
    /// <returns>The puddle if one was created, null otherwise.</returns>
    public PuddleComponent? SpillAt(Solution solution, EntityCoordinates coordinates, string prototype,
        bool overflow = true, bool sound = true, bool combine = true)
    {
        if (solution.TotalVolume == 0) return null;


        if (!_mapManager.TryGetGrid(coordinates.GetGridId(EntityManager), out var mapGrid))
            return null; // Let's not spill to space.

        return SpillAt(mapGrid.GetTileRef(coordinates), solution, prototype, overflow, sound,
            combine: combine);
    }

    public bool TryGetPuddle(TileRef tileRef, [NotNullWhen(true)] out PuddleComponent? puddle)
    {
        foreach (var entity in _entityLookup.GetEntitiesIntersecting(tileRef))
        {
            if (EntityManager.TryGetComponent(entity, out PuddleComponent? p))
            {
                puddle = p;
                return true;
            }
        }

        puddle = null;
        return false;
    }

    public PuddleComponent? SpillAt(TileRef tileRef, Solution solution, string prototype,
        bool overflow = true, bool sound = true, bool noTileReact = false, bool combine = true)
    {
        if (solution.TotalVolume <= 0) return null;

        // If space return early, let that spill go out into the void
        if (tileRef.Tile.IsEmpty) return null;

        var gridId = tileRef.GridIndex;
        if (!_mapManager.TryGetGrid(gridId, out var mapGrid)) return null; // Let's not spill to invalid grids.

        if (!noTileReact)
        {
            // First, do all tile reactions
            foreach (var (reagentId, quantity) in solution.Contents)
            {
                var proto = _prototypeManager.Index<ReagentPrototype>(reagentId);
                proto.ReactionTile(tileRef, quantity);
            }
        }

        // Tile reactions used up everything.
        if (solution.CurrentVolume == FixedPoint2.Zero)
            return null;

        // Get normalized co-ordinate for spill location and spill it in the centre
        // TODO: Does SnapGrid or something else already do this?
        var spillGridCoords = mapGrid.GridTileToWorld(tileRef.GridIndices);

        var spillEntities = _entityLookup.GetEntitiesIntersecting(mapGrid.ParentMapId, spillGridCoords.Position).ToArray();
        foreach (var spillEntity in spillEntities)
        {
            if (_solutionContainerSystem.TryGetRefillableSolution(spillEntity, out var solutionContainerComponent))
            {
                _solutionContainerSystem.Refill(spillEntity, solutionContainerComponent,
                    solution.SplitSolution(FixedPoint2.Min(
                        solutionContainerComponent.AvailableVolume,
                        solutionContainerComponent.MaxSpillRefill))
                );
            }
        }

        if (combine)
        {
            foreach (var spillEntity in spillEntities)
            {
                if (!EntityManager.TryGetComponent(spillEntity, out PuddleComponent? puddleComponent)) continue;

                if (!overflow && _puddleSystem.WouldOverflow(puddleComponent.Owner, solution, puddleComponent))
                    return null;

                if (!_puddleSystem.TryAddSolution(puddleComponent.Owner, solution, sound)) continue;

                return puddleComponent;
            }
        }

        var puddleEnt = EntityManager.SpawnEntity(prototype, spillGridCoords);
        var newPuddleComponent = EntityManager.GetComponent<PuddleComponent>(puddleEnt);

        _puddleSystem.TryAddSolution(newPuddleComponent.Owner, solution, sound);

        return newPuddleComponent;
    }
}
