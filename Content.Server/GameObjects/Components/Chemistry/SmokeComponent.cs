using System;
using System.Composition.Hosting.Core;
using System.Linq;
using System.Net.Security;
using Content.Server.GameObjects.Components.Atmos;
using Content.Server.GameObjects.Components.Body.Circulatory;
using Content.Server.GameObjects.Components.Body.Respiratory;
using Content.Server.Utility;
using Content.Shared.Chemistry;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Timers;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Chemistry
{
    [RegisterComponent]
    public class SmokeComponent : Component
    {
        public override string Name => "Smoke";

        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        private IEntity _inception;

        private bool _running;
        private float _timer;
        private float _exposetimer;

        private float _exposeDelay;

        private int _amount;
        private float _lifetime;
        private float _spreadDelay;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _exposeDelay, "exposeDelay", 0.5f);
        }

        public void Start(IEntity inception, int amount, float lifetime, float spreadDelay)
        {
            _inception = inception;
            _amount = amount;
            _lifetime = lifetime;
            _spreadDelay = spreadDelay;
            _running = true;

            Owner.SpawnTimer((int) (_spreadDelay*1000), SpreadSmoke);
        }

        public void Update(float frameTime)
        {
            if (!_running)
                return;

            _timer += frameTime;
            _exposetimer += frameTime;

            if (_exposetimer > _exposeDelay)
            {
                _exposetimer = 0;
                ReactWithTileAndEntities();
            }

            if (_timer > _lifetime)
            {
                Owner.Delete();
            }
        }

        public bool TryAddSolution(Solution solution)
        {
            if (solution.TotalVolume == 0)
            {
                return false;
            }

            if (!Owner.TryGetComponent(out SolutionContainerComponent contents))
            {
                return false;
            }

            var result = contents.TryAddSolution(solution);

            if (!result)
            {
                return false;
            }

            if (Owner.TryGetComponent(out SpriteComponent sprite))
                sprite.Color = contents.SubstanceColor;

            return true;
        }
        private void SpreadSmoke()
        {
            if (_amount == 0)
            {
                return;
            }

            if (!Owner.TryGetComponent(out SnapGridComponent snapGrid))
                return;

            void SpreadToDir(Direction dir)
            {
                foreach (var neighbor in snapGrid.GetInDir(dir))
                {
                    if (neighbor.TryGetComponent(out SmokeComponent comp) && comp._inception == _inception)
                        return;

                    if (neighbor.TryGetComponent(out AirtightComponent airtight) && airtight.AirBlocked)
                        return;
                }
                var newSmoke = Owner.EntityManager.SpawnEntity("smoke_cloud", snapGrid.DirectionToGrid(dir));
                var smokeComponent = newSmoke.GetComponent<SmokeComponent>();

                if (Owner.TryGetComponent(out SolutionContainerComponent contents))
                {
                    var solution = contents.Solution.Clone();
                    smokeComponent.TryAddSolution(solution);
                }
                smokeComponent.Start(_inception,_amount-1,_lifetime-_timer-_spreadDelay, _spreadDelay);
            }

            SpreadToDir(Direction.North);
            SpreadToDir(Direction.East);
            SpreadToDir(Direction.South);
            SpreadToDir(Direction.West);
        }

        private void ReactWithTileAndEntities()
        {
            if (!Owner.TryGetComponent(out SolutionContainerComponent contents))
                return;

            var mapGrid = _mapManager.GetGrid(Owner.Transform.GridID);
            var tile = mapGrid.GetTileRef(Owner.Transform.Coordinates.ToVector2i(Owner.EntityManager, _mapManager));

            var solutionFraction = 1 / Math.Floor(_lifetime / _exposeDelay);

            // Reagents react with the tile under the smoke
            foreach (var reagentQuantity in contents.ReagentList.ToArray())
            {
                if (reagentQuantity.Quantity == ReagentUnit.Zero) continue;
                var reagent = _prototypeManager.Index<ReagentPrototype>(reagentQuantity.ReagentId);
                reagent.ReactionTile(tile, reagentQuantity.Quantity * solutionFraction);
            }

            foreach (var entity in tile.GetEntitiesInTileFast())
            {
                // Reagents touch all entities on the tile
                foreach (var reagentQuantity in contents.ReagentList.ToArray())
                {
                    if (reagentQuantity.Quantity == ReagentUnit.Zero) continue;
                    var reagent = _prototypeManager.Index<ReagentPrototype>(reagentQuantity.ReagentId);
                    reagent.ReactionEntity(entity, ReactionMethod.Touch, reagentQuantity.Quantity * solutionFraction);
                }

                // Reagents enter the bloodstream of all entities without internals.
                if (!entity.TryGetComponent(out BloodstreamComponent bloodstream))
                    continue;

                if (entity.TryGetComponent(out InternalsComponent internals) &&
                    internals.BreathToolEntity != null &&
                    internals.GasTankEntity != null &&
                    internals.BreathToolEntity.TryGetComponent(out BreathToolComponent breathTool) &&
                    breathTool.IsFunctional &&
                    internals.GasTankEntity.TryGetComponent(out GasTankComponent gasTank) &&
                    gasTank.Air != null)
                    continue;

                var cloneSolution = contents.Solution.Clone();
                var transferAmount = ReagentUnit.Min(cloneSolution.TotalVolume * solutionFraction, bloodstream.EmptyVolume);
                var transferSolution = cloneSolution.SplitSolution(transferAmount);

                foreach (var reagentQuantity in transferSolution.Contents.ToArray())
                {
                    if (reagentQuantity.Quantity == ReagentUnit.Zero) continue;
                    var reagent = _prototypeManager.Index<ReagentPrototype>(reagentQuantity.ReagentId);
                    transferSolution.RemoveReagent(reagentQuantity.ReagentId,reagent.ReactionEntity(entity, ReactionMethod.Ingestion, reagentQuantity.Quantity));
                }

                bloodstream.TryTransferSolution(transferSolution);
            }
        }
    }
}
