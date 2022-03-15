using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Construction.Components;
using Content.Server.Fluids.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Database;
using Content.Shared.Directions;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Slippery;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Player;

namespace Content.Server.Fluids.EntitySystems
{
    [UsedImplicitly]
    public sealed class PuddleSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PuddleComponent, AnchorStateChangedEvent>(OnAnchorChanged);
            SubscribeLocalEvent<PuddleComponent, ExaminedEvent>(HandlePuddleExamined);
            SubscribeLocalEvent<PuddleComponent, SolutionChangedEvent>(OnUpdate);
            SubscribeLocalEvent<PuddleComponent, ComponentInit>(OnInit);
        }

        private void OnInit(EntityUid uid, PuddleComponent component, ComponentInit args)
        {
            var solution =  _solutionContainerSystem.EnsureSolution(uid, component.SolutionName);
            solution.MaxVolume = FixedPoint2.New(1000);
        }

        private void OnUpdate(EntityUid uid, PuddleComponent component, SolutionChangedEvent args)
        {
            UpdateSlip(uid, component);
            UpdateVisuals(uid, component);
        }

        private void UpdateVisuals(EntityUid uid, PuddleComponent puddleComponent)
        {
            if (Deleted(puddleComponent.Owner) || EmptyHolder(uid, puddleComponent) ||
                !EntityManager.TryGetComponent<AppearanceComponent>(uid, out var appearanceComponent))
            {
                return;
            }

            // Opacity based on level of fullness to overflow
            // Hard-cap lower bound for visibility reasons
            var volumeScale = puddleComponent.CurrentVolume.Float() / puddleComponent.OverflowVolume.Float();
            var puddleSolution = _solutionContainerSystem.EnsureSolution(uid, puddleComponent.SolutionName);

            appearanceComponent.SetData(PuddleVisuals.VolumeScale, volumeScale);
            appearanceComponent.SetData(PuddleVisuals.SolutionColor, puddleSolution.Color);
        }

        private void UpdateSlip(EntityUid entityUid, PuddleComponent puddleComponent)
        {
            if ((puddleComponent.SlipThreshold == FixedPoint2.New(-1) ||
                 puddleComponent.CurrentVolume < puddleComponent.SlipThreshold) &&
                EntityManager.TryGetComponent(entityUid, out SlipperyComponent? oldSlippery))
            {
                oldSlippery.Slippery = false;
            }
            else if (puddleComponent.CurrentVolume >= puddleComponent.SlipThreshold)
            {
                var newSlippery = EntityManager.EnsureComponent<SlipperyComponent>(entityUid);
                newSlippery.Slippery = true;
            }
        }

        private void HandlePuddleExamined(EntityUid uid, PuddleComponent component, ExaminedEvent args)
        {
            if (EntityManager.TryGetComponent<SlipperyComponent>(uid, out var slippery) && slippery.Slippery)
            {
                args.PushText(Loc.GetString("puddle-component-examine-is-slipper-text"));
            }
        }

        private void OnAnchorChanged(EntityUid uid, PuddleComponent puddle, ref AnchorStateChangedEvent args)
        {
            if (!args.Anchored)
                QueueDel(uid);
        }

        /// <summary>
        ///     Whether adding this solution to this puddle would overflow.
        /// </summary>
        /// <param name="uid">Uid of owning entity</param>
        /// <param name="puddle">Puddle to which we are adding solution</param>
        /// <param name="solution">Solution we intend to add</param>
        /// <returns></returns>
        public bool WouldOverflow(EntityUid uid, Solution solution, PuddleComponent? puddle = null)
        {
            if (!Resolve(uid, ref puddle))
                return false;

            return puddle.CurrentVolume + solution.TotalVolume > puddle.OverflowVolume;
        }

        public bool EmptyHolder(EntityUid uid, PuddleComponent? puddleComponent = null)
        {
            if (!Resolve(uid, ref puddleComponent))
                return true;

            return !_solutionContainerSystem.TryGetSolution(puddleComponent.Owner, puddleComponent.SolutionName,
                       out var solution)
                   || solution.Contents.Count == 0;
        }

        public FixedPoint2 CurrentVolume(EntityUid uid, PuddleComponent? puddleComponent = null)
        {
            if (!Resolve(uid, ref puddleComponent))
                return FixedPoint2.Zero;

            return _solutionContainerSystem.TryGetSolution(puddleComponent.Owner, puddleComponent.SolutionName,
                out var solution)
                ? solution.CurrentVolume
                : FixedPoint2.Zero;
        }

        public bool TryAddSolution(EntityUid uid, Solution solution,
            bool sound = true,
            bool checkForOverflow = true,
            PuddleComponent? puddleComponent = null)
        {
            if (!Resolve(uid, ref puddleComponent))
                return false;

            if (solution.TotalVolume == 0 ||
                !_solutionContainerSystem.TryGetSolution(puddleComponent.Owner, puddleComponent.SolutionName,
                    out var puddleSolution))
            {
                return false;
            }


            var result = _solutionContainerSystem
                .TryAddSolution(puddleComponent.Owner, puddleSolution, solution);
            if (!result)
            {
                return false;
            }

            RaiseLocalEvent(puddleComponent.Owner, new SolutionChangedEvent());

            if (checkForOverflow)
            {
                CheckOverflow(puddleComponent);
            }

            if (!sound)
            {
                return true;
            }

            SoundSystem.Play(Filter.Pvs(puddleComponent.Owner), puddleComponent.SpillSound.GetSound(),
                puddleComponent.Owner);
            return true;
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

            while (puddleComponent.OverflowLeft > FixedPoint2.Zero && nextPuddles.Count > 0)
            {
                foreach (var next in nextPuddles.ToArray())
                {
                    nextPuddles.Remove(next);

                    next.Overflown = true;
                    overflownPuddles.Add(next);

                    var adjacentPuddles = GetAllAdjacentOverflow(next).ToArray();
                    if (puddleComponent.OverflowLeft <= FixedPoint2.Epsilon * adjacentPuddles.Length)
                    {
                        break;
                    }

                    if (adjacentPuddles.Length == 0)
                    {
                        continue;
                    }

                    var numberOfAdjacent = FixedPoint2.New(adjacentPuddles.Length);
                    var overflowSplit = puddleComponent.OverflowLeft / numberOfAdjacent;
                    foreach (var adjacent in adjacentPuddles)
                    {
                        var adjacentPuddle = adjacent();
                        var quantity = FixedPoint2.Min(overflowSplit, adjacentPuddle.OverflowVolume);
                        var puddleSolution = _solutionContainerSystem.EnsureSolution(puddleComponent.Owner,
                            puddleComponent.SolutionName);
                        var spillAmount = _solutionContainerSystem.SplitSolution(puddleComponent.Owner,
                            puddleSolution, quantity);

                        TryAddSolution(adjacentPuddle.Owner, spillAmount, false, false);
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
            if (!EntityManager.GetComponent<TransformComponent>(puddleComponent.Owner).GridID.IsValid())
                return false;

            var mapGrid = _mapManager.GetGrid(EntityManager.GetComponent<TransformComponent>(puddleComponent.Owner).GridID);
            var coords = EntityManager.GetComponent<TransformComponent>(puddleComponent.Owner).Coordinates;

            if (!coords.Offset(direction).TryGetTileRef(out var tile))
            {
                return false;
            }

            // If space return early, let that spill go out into the void
            if (tile.Value.Tile.IsEmpty)
            {
                return false;
            }

            if (!EntityManager.GetComponent<TransformComponent>(puddleComponent.Owner).Anchored)
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
            {
                var id = EntityManager.SpawnEntity(
                    EntityManager.GetComponent<MetaDataComponent>(puddleComponent.Owner).EntityPrototype?.ID,
                    mapGrid.DirectionToGrid(coords, direction));
                return EntityManager.GetComponent<PuddleComponent>(id);
            };

            return true;
        }
    }
}
