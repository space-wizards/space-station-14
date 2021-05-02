#nullable enable
using System;
using System.Linq;
using Content.Server.GameObjects.Components.Atmos;
using Content.Server.Utility;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Server.GameObjects.Components.Chemistry
{
    /// <summary>
    /// Used to clone its owner repeatedly and group up them all so they behave like one unit, that way you can have
    /// effects that cover an area. Inherited by <see cref="SmokeSolutionAreaEffectComponent"/> and <see cref="FoamSolutionAreaEffectComponent"/>.
    /// </summary>
    public abstract class SolutionAreaEffectComponent : Component
    {
        [Dependency] protected readonly IMapManager MapManager = default!;
        [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;

        [ComponentDependency] protected readonly SolutionContainerComponent? SolutionContainerComponent = default!;
        public int Amount { get; set; }
        public SolutionAreaEffectInceptionComponent? Inception { get; set; }

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

            if (Owner.HasComponent<SolutionAreaEffectInceptionComponent>())
                return;

            Amount = amount;
            var inception = Owner.AddComponent<SolutionAreaEffectInceptionComponent>();

            inception.Add(this);
            inception.Setup(amount, duration, spreadDelay, removeDelay);
        }

        /// <summary>
        /// Gets called by an AreaEffectInceptionComponent. "Clones" Owner into the four directions and copies the
        /// solution into each of them.
        /// </summary>
        public void Spread()
        {
            if (Owner.Prototype == null)
            {
                Logger.Error("AreaEffectComponent needs its owner to be spawned by a prototype.");
                return;
            }

            void SpreadToDir(Direction dir)
            {
                var grid = MapManager.GetGrid(Owner.Transform.GridID);
                var coords = Owner.Transform.Coordinates;
                foreach (var neighbor in grid.GetInDir(coords, dir))
                {
                    if (Owner.EntityManager.ComponentManager.TryGetComponent(neighbor, out SolutionAreaEffectComponent? comp) && comp.Inception == Inception)
                        return;

                    if (Owner.EntityManager.ComponentManager.TryGetComponent(neighbor, out AirtightComponent? airtight) && airtight.AirBlocked)
                        return;
                }

                var newEffect = Owner.EntityManager.SpawnEntity(Owner.Prototype.ID, grid.DirectionToGrid(coords, dir));

                if (!newEffect.TryGetComponent(out SolutionAreaEffectComponent? effectComponent))
                {
                    newEffect.Delete();
                    return;
                }

                if (SolutionContainerComponent != null)
                {
                    effectComponent.TryAddSolution(SolutionContainerComponent.Solution.Clone());
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
            if (SolutionContainerComponent == null)
                return;

            var chemistry = EntitySystem.Get<ChemistrySystem>();
            var mapGrid = MapManager.GetGrid(Owner.Transform.GridID);
            var tile = mapGrid.GetTileRef(Owner.Transform.Coordinates.ToVector2i(Owner.EntityManager, MapManager));

            var solutionFraction = 1 / Math.Floor(averageExposures);

            foreach (var reagentQuantity in SolutionContainerComponent.ReagentList.ToArray())
            {
                if (reagentQuantity.Quantity == ReagentUnit.Zero) continue;
                var reagent = PrototypeManager.Index<ReagentPrototype>(reagentQuantity.ReagentId);

                // React with the tile the effect is on
                reagent.ReactionTile(tile, reagentQuantity.Quantity * solutionFraction);

                // Touch every entity on the tile
                foreach (var entity in tile.GetEntitiesInTileFast().ToArray())
                {
                    chemistry.ReactionEntity(entity, ReactionMethod.Touch, reagent, reagentQuantity.Quantity * solutionFraction, SolutionContainerComponent.Solution);
                }
            }

            foreach (var entity in tile.GetEntitiesInTileFast().ToArray())
            {
                ReactWithEntity(entity, solutionFraction);
            }
        }

        protected abstract void ReactWithEntity(IEntity entity, double solutionFraction);

        public void TryAddSolution(Solution solution)
        {
            if (solution.TotalVolume == 0)
                return;

            if (SolutionContainerComponent == null)
                return;

            var addSolution =
                solution.SplitSolution(ReagentUnit.Min(solution.TotalVolume, SolutionContainerComponent.EmptyVolume));

            SolutionContainerComponent.TryAddSolution(addSolution);

            UpdateVisuals();
        }

        protected abstract void UpdateVisuals();

        public override void OnRemove()
        {
            base.OnRemove();
            Inception?.Remove(this);
        }
    }
}
