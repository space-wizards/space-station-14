using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Fluids.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Directions;
using Content.Shared.Examine;
using Content.Shared.Fluids;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Slippery;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Content.Server.Fluids.EntitySystems
{
    [UsedImplicitly]
    public partial class PuddleSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            _mapManager.TileChanged += HandleTileChanged;

            SubscribeLocalEvent<PuddleComponent, ExaminedEvent>(HandlePuddleExamined);
            SubscribeLocalEvent<PuddleComponent, ComponentInit>(OnPuddleInit);
            SubscribeLocalEvent<PuddleComponent, SolutionChangedEvent>(OnPuddleUpdate);
        }
        
        private void OnPuddleUpdate(EntityUid uid, PuddleComponent component, SolutionChangedEvent args)
        {
            UpdateSlip(uid, component);
            UpdateVisuals(uid, component);
        }

        private void OnPuddleInit(EntityUid uid, PuddleComponent component, ComponentInit args)
        {
            var puddleSolution =
                _solutionContainerSystem.EnsureSolution(component.Owner, PuddleComponent.DefaultSolutionName);

            // Smaller than 1m^3 for now but realistically this shouldn't be hit
            puddleSolution.MaxVolume = ReagentUnit.New(1000);

            // UpdateAppearance should get called soon after this so shouldn't need to call Dirty() here
            UpdateVisuals(uid, component);
        }

        private void UpdateVisuals(EntityUid uid, PuddleComponent puddleComponent)
        {
            if (puddleComponent.Owner.Deleted || puddleComponent.EmptyHolder ||
                !EntityManager.TryGetComponent<SharedAppearanceComponent>(uid, out var appearanceComponent))
            {
                return;
            }

            // Opacity based on level of fullness to overflow
            // Hard-cap lower bound for visibility reasons
            var volumeScale = puddleComponent.CurrentVolume.Float() / puddleComponent.OverflowVolume.Float();
            var puddleSolution = _solutionContainerSystem.EnsureSolution(uid, puddleComponent.SolutionName);

            appearanceComponent.SetData(PuddleVisual.VolumeScale, volumeScale);
            appearanceComponent.SetData(PuddleVisual.SolutionColor, puddleSolution.Color);
        }

        /// <summary>
        /// Tries to get an adjacent coordinate to overflow to, unless it is blocked by a wall on the
        /// same tile or the tile is empty
        /// </summary>
        /// <param name="puddleComponent"></param>
        /// <param name="direction">The direction to get the puddle from, respective to this one</param>
        /// <param name="puddle">The puddle that was found or is to be created, or null if there
        /// is a wall in the way</param>
        /// <returns>true if a puddle was found or created, false otherwise</returns>
        private bool TryGetAdjacentOverflow(PuddleComponent puddleComponent, Direction direction,
            [NotNullWhen(true)] out Func<PuddleComponent>? puddle)
        {
            puddle = default;

            // We're most likely in space, do nothing.
            if (!puddleComponent.Owner.Transform.GridID.IsValid())
                return false;

            var mapGrid = _mapManager.GetGrid(puddleComponent.Owner.Transform.GridID);
            var coords = puddleComponent.Owner.Transform.Coordinates;

            if (!coords.Offset(direction).TryGetTileRef(out var tile))
            {
                return false;
            }

            // If space return early, let that spill go out into the void
            if (tile.Value.Tile.IsEmpty)
            {
                return false;
            }

            if (!puddleComponent.Owner.Transform.Anchored)
                return false;

            foreach (var entity in mapGrid.GetInDir(coords, direction))
            {
                if (EntityManager.TryGetComponent(entity, out IPhysBody? physics) &&
                    (physics.CollisionLayer & (int)CollisionGroup.Impassable) != 0)
                {
                    puddle = default;
                    return false;
                }

                if (EntityManager.TryGetComponent(entity, out PuddleComponent? existingPuddle))
                {
                    if (existingPuddle.Overflown)
                    {
                        return false;
                    }

                    puddle = () => existingPuddle;
                }
            }

            puddle ??= () =>
                puddleComponent.Owner.EntityManager.SpawnEntity(puddleComponent.Owner.Prototype?.ID,
                        mapGrid.DirectionToGrid(coords, direction))
                    .GetComponent<PuddleComponent>();

            return true;
        }

        private void UpdateSlip(EntityUid entityUid, PuddleComponent puddleComponent)
        {
            if ((puddleComponent.SlipThreshold == ReagentUnit.New(-1) ||
                 puddleComponent.CurrentVolume < puddleComponent.SlipThreshold) &&
                EntityManager.TryGetComponent(entityUid, out SlipperyComponent? oldSlippery))
            {
                oldSlippery.Slippery = false;
            }
            else if (puddleComponent.CurrentVolume >= puddleComponent.SlipThreshold)
            {
                var newSlippery =
                    EntityManager.EnsureComponent<SlipperyComponent>(EntityManager.GetEntity(entityUid));
                newSlippery.Slippery = true;
            }
        }

        public void SplitSolution(EntityUid entityUid, PuddleComponent puddleComponent, ReagentUnit quantity)
        {
            if (!_solutionContainerSystem.TryGetSolution(entityUid, puddleComponent.SolutionName, out var solution))
                return;

            _solutionContainerSystem.SplitSolution(entityUid, solution, quantity);

            RaiseLocalEvent(entityUid, new SolutionChangedEvent());
        }

        /// <summary>
        /// Will overflow this entity to neighboring entities if required
        /// </summary>
        private void CheckOverflow(PuddleComponent puddleComponent)
        {
            if (puddleComponent.CurrentVolume <= puddleComponent.OverflowVolume
                || puddleComponent.Overflown)
                return;

            var nextPuddles = new List<PuddleComponent>() { puddleComponent };
            var overflownPuddles = new List<PuddleComponent>();

            while (puddleComponent.OverflowLeft > ReagentUnit.Zero && nextPuddles.Count > 0)
            {
                foreach (var next in nextPuddles.ToArray())
                {
                    nextPuddles.Remove(next);

                    next.Overflown = true;
                    overflownPuddles.Add(next);

                    var adjacentPuddles = GetAllAdjacentOverflow(next).ToArray();
                    if (puddleComponent.OverflowLeft <= ReagentUnit.Epsilon * adjacentPuddles.Length)
                    {
                        break;
                    }

                    if (adjacentPuddles.Length == 0)
                    {
                        continue;
                    }

                    var numberOfAdjacent = ReagentUnit.New(adjacentPuddles.Length);
                    var overflowSplit = puddleComponent.OverflowLeft / numberOfAdjacent;
                    foreach (var adjacent in adjacentPuddles)
                    {
                        var adjacentPuddle = adjacent();
                        var quantity = ReagentUnit.Min(overflowSplit, adjacentPuddle.OverflowVolume);
                        var puddleSolution = _solutionContainerSystem.EnsureSolution(puddleComponent.Owner.Uid,
                            puddleComponent.SolutionName);
                        var spillAmount = _solutionContainerSystem.SplitSolution(puddleComponent.Owner.Uid,
                            puddleSolution, quantity);

                        TryAddSolution(adjacentPuddle, spillAmount, false, false);
                        nextPuddles.Add(adjacentPuddle);
                    }
                }
            }

            foreach (var puddle in overflownPuddles)
            {
                puddle.Overflown = false;
            }
        }

        /// <summary>
        /// Finds or creates adjacent puddles in random directions from this one
        /// </summary>
        /// <returns>Enumerable of the puddles found or to be created</returns>
        private IEnumerable<Func<PuddleComponent>> GetAllAdjacentOverflow(PuddleComponent puddleComponent)
        {
            foreach (var direction in SharedDirectionExtensions.RandomDirections())
            {
                if (TryGetAdjacentOverflow(puddleComponent, direction, out var puddle))
                {
                    yield return puddle;
                }
            }
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _mapManager.TileChanged -= HandleTileChanged;
        }

        private void HandlePuddleExamined(EntityUid uid, PuddleComponent component, ExaminedEvent args)
        {
            if (EntityManager.TryGetComponent<SlipperyComponent>(uid, out var slippery) && slippery.Slippery)
            {
                args.PushText(Loc.GetString("puddle-component-examine-is-slipper-text"));
            }
        }

        //TODO: Replace all this with an Unanchored event that deletes the puddle
        private void HandleTileChanged(object? sender, TileChangedEventArgs eventArgs)
        {
            // If this gets hammered you could probably queue up all the tile changes every tick but I doubt that would ever happen.
            foreach (var puddle in EntityManager.EntityQuery<PuddleComponent>(true))
            {
                // If the tile becomes space then delete it (potentially change by design)
                var puddleTransform = puddle.Owner.Transform;
                if (!puddleTransform.Anchored)
                    continue;

                var grid = _mapManager.GetGrid(puddleTransform.GridID);
                if (eventArgs.NewTile.GridIndex == puddle.Owner.Transform.GridID &&
                    grid.TileIndicesFor(puddleTransform.Coordinates) == eventArgs.NewTile.GridIndices &&
                    eventArgs.NewTile.Tile.IsEmpty)
                {
                    puddle.Owner.QueueDelete();
                    break; // Currently it's one puddle per tile, if that changes remove this
                }
            }
        }

        public bool EmptyHolder(PuddleComponent puddleComponent)
        {
            return !_solutionContainerSystem.TryGetSolution(puddleComponent.Owner.Uid, puddleComponent.SolutionName,
                           out var solution)
                   || solution.Contents.Count == 0;
        }

        public ReagentUnit CurrentVolume(PuddleComponent puddleComponent)
        {
            return _solutionContainerSystem.TryGetSolution(puddleComponent.Owner.Uid, puddleComponent.SolutionName,
                out var solution)
                ? solution.CurrentVolume
                : ReagentUnit.Zero;
        }
    }
}