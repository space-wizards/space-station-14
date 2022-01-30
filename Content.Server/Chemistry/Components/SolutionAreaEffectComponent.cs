using System;
using System.Linq;
using Content.Server.Atmos.Components;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Coordinates.Helpers;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.Components
{
    /// <summary>
    /// Used to clone its owner repeatedly and group up them all so they behave like one unit, that way you can have
    /// effects that cover an area. Inherited by <see cref="SmokeSolutionAreaEffectComponent"/> and <see cref="FoamSolutionAreaEffectComponent"/>.
    /// </summary>
    public abstract class SolutionAreaEffectComponent : Component
    {
        public const string SolutionName = "solutionArea";

        [Dependency] protected readonly IMapManager MapManager = default!;
        [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
        [Dependency] private readonly IEntityManager _entities = default!;

        public int Amount { get; set; }
        public SolutionAreaEffectInceptionComponent? Inception { get; set; }

        /// <summary>
        ///     Have we reacted with our tile yet?
        /// </summary>
        public bool ReactedTile = false;

        /// <summary>
        /// Adds an <see cref="SolutionAreaEffectInceptionComponent"/> to owner so the effect starts spreading and reacting.
        /// </summary>
        /// <param name="amount">The range of the effect</param>
        /// <param name="duration"></param>
        /// <param name="spreadDelay"></param>
        /// <param name="removeDelay"></param>
        public void Start(int amount, float duration, float spreadDelay, float removeDelay)
        {
            if (Inception != null)
                return;

            if (_entities.HasComponent<SolutionAreaEffectInceptionComponent>(Owner))
                return;

            Amount = amount;
            var inception = _entities.AddComponent<SolutionAreaEffectInceptionComponent>(Owner);

            inception.Add(this);
            inception.Setup(amount, duration, spreadDelay, removeDelay);
        }

        /// <summary>
        /// Gets called by an AreaEffectInceptionComponent. "Clones" Owner into the four directions and copies the
        /// solution into each of them.
        /// </summary>
        public void Spread()
        {
            if (_entities.GetComponent<MetaDataComponent>(Owner).EntityPrototype == null)
            {
                Logger.Error("AreaEffectComponent needs its owner to be spawned by a prototype.");
                return;
            }

            void SpreadToDir(Direction dir)
            {
                var grid = MapManager.GetGrid(_entities.GetComponent<TransformComponent>(Owner).GridID);
                var coords = _entities.GetComponent<TransformComponent>(Owner).Coordinates;
                foreach (var neighbor in grid.GetInDir(coords, dir))
                {
                    if (_entities.TryGetComponent(neighbor,
                        out SolutionAreaEffectComponent? comp) && comp.Inception == Inception)
                        return;

                    if (_entities.TryGetComponent(neighbor,
                        out AirtightComponent? airtight) && airtight.AirBlocked)
                        return;
                }

                var newEffect = _entities.SpawnEntity(
                    _entities.GetComponent<MetaDataComponent>(Owner).EntityPrototype?.ID,
                    grid.DirectionToGrid(coords, dir));

                if (!_entities.TryGetComponent(newEffect, out SolutionAreaEffectComponent? effectComponent))
                {
                    _entities.DeleteEntity(newEffect);
                    return;
                }

                if (EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(Owner, SolutionName, out var solution))
                {
                    effectComponent.TryAddSolution(solution.Clone());
                }

                effectComponent.Amount = Amount - 1;
                Inception?.Add(effectComponent);
            }

            SpreadToDir(Direction.North);
            SpreadToDir(Direction.East);
            SpreadToDir(Direction.South);
            SpreadToDir(Direction.West);
        }

        /// <summary>
        /// Gets called by an AreaEffectInceptionComponent.
        /// Removes this component from its inception and calls OnKill(). The implementation of OnKill() should
        /// eventually delete the entity.
        /// </summary>
        public void Kill()
        {
            Inception?.Remove(this);
            OnKill();
        }

        protected abstract void OnKill();

        /// <summary>
        /// Gets called by an AreaEffectInceptionComponent.
        /// Makes this effect's reagents react with the tile its on and with the entities it covers. Also calls
        /// ReactWithEntity on the entities so inheritors can implement more specific behavior.
        /// </summary>
        /// <param name="averageExposures">How many times will this get called over this area effect's duration, averaged
        /// with the other area effects from the inception.</param>
        public void React(float averageExposures)
        {
            if (!EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(Owner, SolutionName, out var solution))
                return;

            var chemistry = EntitySystem.Get<ReactiveSystem>();
            var xform = _entities.GetComponent<TransformComponent>(Owner);
            var mapGrid = MapManager.GetGrid(xform.GridID);
            var tile = mapGrid.GetTileRef(xform.Coordinates.ToVector2i(_entities, MapManager));
            var lookup = IoCManager.Resolve<IEntityLookup>();

            var solutionFraction = 1 / Math.Floor(averageExposures);

            foreach (var reagentQuantity in solution.Contents.ToArray())
            {
                if (reagentQuantity.Quantity == FixedPoint2.Zero) continue;
                var reagent = PrototypeManager.Index<ReagentPrototype>(reagentQuantity.ReagentId);

                // React with the tile the effect is on
                // We don't multiply by solutionFraction here since the tile is only ever reacted once
                if (!ReactedTile)
                {
                    reagent.ReactionTile(tile, reagentQuantity.Quantity);
                    ReactedTile = true;
                }

                // Touch every entity on the tile
                foreach (var entity in lookup.GetEntitiesIntersecting(tile).ToArray())
                {
                    chemistry.ReactionEntity(entity, ReactionMethod.Touch, reagent,
                        reagentQuantity.Quantity * solutionFraction, solution);
                }
            }

            foreach (var entity in lookup.GetEntitiesIntersecting(tile).ToArray())
            {
                ReactWithEntity(entity, solutionFraction);
            }
        }

        protected abstract void ReactWithEntity(EntityUid entity, double solutionFraction);

        public void TryAddSolution(Solution solution)
        {
            if (solution.TotalVolume == 0)
                return;

            if (!EntitySystem.Get<SolutionContainerSystem>().TryGetSolution(Owner, SolutionName, out var solutionArea))
                return;

            var addSolution =
                solution.SplitSolution(FixedPoint2.Min(solution.TotalVolume, solutionArea.AvailableVolume));

            EntitySystem.Get<SolutionContainerSystem>().TryAddSolution(Owner, solutionArea, addSolution);

            UpdateVisuals();
        }

        protected abstract void UpdateVisuals();

        protected override void OnRemove()
        {
            base.OnRemove();
            Inception?.Remove(this);
        }
    }
}
