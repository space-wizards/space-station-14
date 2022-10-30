using Content.Server.Chemistry.EntitySystems;
using Content.Server.Fluids.Components;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
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
                var uid = evaporationComponent.Owner;
                evaporationComponent.Accumulator += frameTime;

                if (!_solutionContainerSystem.TryGetSolution(uid, evaporationComponent.SolutionName, out var solution))
                {
                    // If no solution, delete the entity
                    EntityManager.QueueDeleteEntity(uid);
                    continue;
                }

                if (evaporationComponent.Accumulator < evaporationComponent.EvaporateTime)
                    continue;

                evaporationComponent.Accumulator -= evaporationComponent.EvaporateTime;

                if (evaporationComponent.EvaporationToggle == true)
                {
                    _solutionContainerSystem.SplitSolution(uid, solution,
                        FixedPoint2.Min(FixedPoint2.New(1), solution.CurrentVolume)); // removes 1 unit, or solution current volume, whichever is lower.
                }

                if (solution.CurrentVolume <= 0)
                {
                    EntityManager.QueueDeleteEntity(uid);
                }
                else if (solution.CurrentVolume <= evaporationComponent.LowerLimit // if puddle is too big or too small to evaporate.
                         || solution.CurrentVolume >= evaporationComponent.UpperLimit)
                {
                    evaporationComponent.EvaporationToggle = false; // pause evaporation
                }
                else evaporationComponent.EvaporationToggle = true; // unpause evaporation, e.g. if a puddle previously above evaporation UpperLimit was brought down below evaporation UpperLimit via mopping.
            }

            foreach (var evaporationComponent in queueDelete)
            {
                EntityManager.RemoveComponent(evaporationComponent.Owner, evaporationComponent);
            }
        }

        /// <summary>
        ///  Copy constructor to copy initial fields from source to destination.
        /// </summary>
        /// <param name="destUid">Entity to which we copy <paramref name="srcEvaporation"/> properties</param>
        /// <param name="srcEvaporation">Component that contains relevant properties</param>
        public void CopyConstruct(EntityUid destUid, EvaporationComponent srcEvaporation)
        {
            var destEvaporation = EntityManager.EnsureComponent<EvaporationComponent>(destUid);
            destEvaporation.EvaporateTime = srcEvaporation.EvaporateTime;
            destEvaporation.EvaporationToggle = srcEvaporation.EvaporationToggle;
            destEvaporation.SolutionName = srcEvaporation.SolutionName;
            destEvaporation.LowerLimit = srcEvaporation.LowerLimit;
            destEvaporation.UpperLimit = srcEvaporation.UpperLimit;
        }
    }
}
