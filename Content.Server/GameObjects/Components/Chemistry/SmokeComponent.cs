using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Atmos;
using Content.Server.GameObjects.Components.Body.Circulatory;
using Content.Server.GameObjects.Components.Body.Respiratory;
using Content.Server.Utility;
using Content.Shared.Chemistry;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Server.GameObjects.Components.Chemistry
{
    [RegisterComponent]
    public class SmokeComponent : Component
    {
        public override string Name => "Smoke";
        public int Amount { get; set; }
        public SmokeInception Inception { get; set; }

        /// <summary>
        /// Creates a smoke inception and adds this SmokeComponent to it.
        /// </summary>
        /// <remarks>
        /// Only works on smokes that don't have an inception.
        /// </remarks>
        public void Activate(Solution solution, int amount, float duration, float spreadDelay)
        {
            if (Inception != null)
                return;

            TryAddSolution(solution);
            Amount = amount;
            var inception = new SmokeInception(amount, duration, spreadDelay);

            inception.AddSmoke(this);

            //Register the inception into the smoke system
            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new SmokeInceptionCreatedMessage(Inception));
        }

        public override void OnRemove()
        {
            base.OnRemove();
            Inception?.RemoveSmoke(this);
        }

        private void TryAddSolution(Solution solution)
        {
            if (solution.TotalVolume == 0)
                return;

            if (!Owner.TryGetComponent(out SolutionContainerComponent contents))
                return;

            var addSolution = solution.SplitSolution(ReagentUnit.Min(solution.TotalVolume,contents.EmptyVolume));

            var result = contents.TryAddSolution(addSolution);

            if (!result)
                return;

            if (Owner.TryGetComponent(out SpriteComponent sprite))
                sprite.Color = contents.SubstanceColor;
        }

        public void SpreadSmoke()
        {
            if (Owner.Prototype?.ID == null)
                return;

            if (!Owner.TryGetComponent(out SnapGridComponent snapGrid))
            {
                Logger.Error("SmokeComponent attached to "+Owner.Prototype?.ID+" couldn't get SnapGridComponent from owner.");
                return;
            }

            void SpreadToDir(Direction dir)
            {
                foreach (var neighbor in snapGrid.GetInDir(dir))
                {
                    if (neighbor.TryGetComponent(out SmokeComponent comp) && comp.Inception == Inception)
                        return;

                    if (neighbor.TryGetComponent(out AirtightComponent airtight) && airtight.AirBlocked)
                        return;
                }
                var newSmoke = Owner.EntityManager.SpawnEntity(Owner.Prototype?.ID, snapGrid.DirectionToGrid(dir));
                if (!newSmoke.TryGetComponent(out SmokeComponent smokeComponent))
                    return;

                if (Owner.TryGetComponent(out SolutionContainerComponent contents))
                {
                    var solution = contents.Solution.Clone();
                    smokeComponent.TryAddSolution(solution);
                }

                smokeComponent.Amount = Amount - 1;
                Inception.AddSmoke(smokeComponent);
            }

            SpreadToDir(Direction.North);
            SpreadToDir(Direction.East);
            SpreadToDir(Direction.South);
            SpreadToDir(Direction.West);
        }

        public void ReactWithTileAndEntities(float averageExpositions, IMapManager mapManager, IPrototypeManager prototypeManager)
        {
            if (!Owner.TryGetComponent(out SolutionContainerComponent contents))
                return;

            var mapGrid = mapManager.GetGrid(Owner.Transform.GridID);
            var tile = mapGrid.GetTileRef(Owner.Transform.Coordinates.ToVector2i(Owner.EntityManager, mapManager));

            var solutionFraction = 1 / Math.Floor(averageExpositions);

            foreach (var reagentQuantity in contents.ReagentList.ToArray())
            {
                if (reagentQuantity.Quantity == ReagentUnit.Zero) continue;
                var reagent = prototypeManager.Index<ReagentPrototype>(reagentQuantity.ReagentId);

                //React with the tile the smoke is on
                reagent.ReactionTile(tile, reagentQuantity.Quantity * solutionFraction);

                foreach (var entity in tile.GetEntitiesInTileFast())
                {
                    //Touch every entity on the tile
                    reagent.ReactionEntity(entity, ReactionMethod.Touch, reagentQuantity.Quantity * solutionFraction);

                    //Enter the bloodstream of every entity without internals
                    if (!entity.TryGetComponent(out BloodstreamComponent bloodstream))
                        continue;

                    if (entity.TryGetComponent(out InternalsComponent internals) &&
                        internals.AreInternalsWorking())
                        continue;

                    var cloneSolution = contents.Solution.Clone();
                    var transferAmount = ReagentUnit.Min(cloneSolution.TotalVolume * solutionFraction, bloodstream.EmptyVolume);
                    var transferSolution = cloneSolution.SplitSolution(transferAmount);

                    transferSolution.RemoveReagent(reagentQuantity.ReagentId,reagent.ReactionEntity(entity, ReactionMethod.Ingestion, reagentQuantity.Quantity));
                    bloodstream.TryTransferSolution(transferSolution);
                }
            }
        }
    }

    public class SmokeInception
        {
            private const float ExposeDelay = 0.5f;

            private readonly HashSet<SmokeComponent> _smokeGroup = new();

            private float _lifeTimer;
            private float _spreadTimer;
            private float _exposeTimer;

            private int _amountCounterSpreading;
            private int _amountCounterRemoving;

            private readonly float _duration;
            private readonly float _spreadDelay;
            private readonly float _averageExpositions;

            public SmokeInception(int amount, float duration, float spreadDelay)
            {
                _amountCounterSpreading = amount;
                _duration = duration;
                _spreadDelay = spreadDelay;
                /*
                The cloud takes amount*spreadDelay seconds to fully spread, same with fully disappearing.
                The outer smokes will last duration seconds.
                The first smoke will last duration + how many seconds the cloud takes to fully spread and fully disappear, so
                it will last duration + 2*amount*spreadDelay.
                Thus, the average lifetime of the smokes will be (outerSmokeLifetime + firstSmokeLifetime)/2 = duration + amount*spreadDelay
                */
                _averageExpositions = (duration + amount * spreadDelay)/ExposeDelay;
            }

            public bool InceptionUpdate(float frameTime, IMapManager mapManager, IPrototypeManager prototypeManager)
            {
                // Job done, return true so the system stops updating it
                if (_smokeGroup.Count == 0)
                    return true;

                // Make every smoke from the group react with the tile and entities
                _exposeTimer += frameTime;
                if (_exposeTimer > ExposeDelay)
                {
                    _exposeTimer -= ExposeDelay;
                    foreach (var smoke in _smokeGroup)
                    {
                        smoke.ReactWithTileAndEntities(_averageExpositions, mapManager, prototypeManager);
                    }
                }

                // Make every outer smoke from the group spread
                if (_amountCounterSpreading != 0)
                {
                    _spreadTimer += frameTime;
                    if (_spreadTimer > _spreadDelay)
                    {
                        _spreadTimer -= _spreadDelay;

                        var outerSmokes = new HashSet<SmokeComponent>(_smokeGroup.Where(smoke => smoke.Amount == _amountCounterSpreading));
                        foreach (var smoke in outerSmokes)
                        {
                            smoke.SpreadSmoke();
                        }

                        _amountCounterSpreading -= 1;
                    }
                }
                // Start counting for _duration after fully spreading
                else
                {
                    _lifeTimer += frameTime;
                }

                // Delete every outer smoke
                if (_lifeTimer > _duration)
                {
                    _spreadTimer += frameTime;
                    if (_spreadTimer > _spreadDelay)
                    {
                        _spreadTimer -= _spreadDelay;

                        var outerSmokes = new HashSet<SmokeComponent>(_smokeGroup.Where(smoke => smoke.Amount == _amountCounterRemoving));
                        foreach (var smoke in outerSmokes)
                        {
                            smoke.Owner.Delete();
                        }

                        _amountCounterRemoving += 1;
                    }
                }

                return false;
            }

            public void AddSmoke(SmokeComponent smoke)
            {
                _smokeGroup.Add(smoke);
                smoke.Inception = this;
            }

            public void RemoveSmoke(SmokeComponent smoke)
            {
                _smokeGroup.Remove(smoke);
                smoke.Inception = null;
            }
        }

    public sealed class SmokeInceptionCreatedMessage : EntitySystemMessage
    {
        public SmokeInception Inception { get; }

        public SmokeInceptionCreatedMessage(SmokeInception inception)
        {
            Inception = inception;
        }
    }
}
