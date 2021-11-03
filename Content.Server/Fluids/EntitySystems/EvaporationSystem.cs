using Content.Server.Chemistry.EntitySystems;
using Content.Server.Fluids.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Content.Server.Fluids.EntitySystems
{
    [UsedImplicitly]
    public sealed class EvaporationSystem : EntitySystem
    {
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            var queueDelete = new RemQueue<EvaporationComponent>();
            foreach (var evaporationComponent in EntityManager.EntityQuery<EvaporationComponent>())
            {
                var uid = evaporationComponent.Owner.Uid;
                evaporationComponent.Accumulator += frameTime;

                if (!_solutionContainerSystem.TryGetSolution(uid, evaporationComponent.SolutionName, out var solution))
                {
                    // If no solution, delete the entity
                    queueDelete.Add(evaporationComponent);
                    continue;
                }

                if (evaporationComponent.Accumulator < evaporationComponent.EvaporateTime)
                    continue;

                evaporationComponent.Accumulator -= evaporationComponent.EvaporateTime;


                _solutionContainerSystem.SplitSolution(uid, solution,
                    FixedPoint2.Min(FixedPoint2.New(1), solution.CurrentVolume));

                if (solution.CurrentVolume == 0)
                {
                    EntityManager.QueueDeleteEntity(uid);
                }
                else if (solution.CurrentVolume <= evaporationComponent.LowerLimit
                         || solution.CurrentVolume >= evaporationComponent.UpperLimit)
                {
                    queueDelete.Add(evaporationComponent);
                }
            }

            foreach (var evaporationComponent in queueDelete)
            {
                EntityManager.RemoveComponent(evaporationComponent.Owner.Uid, evaporationComponent);
            }
        }
    }
}
