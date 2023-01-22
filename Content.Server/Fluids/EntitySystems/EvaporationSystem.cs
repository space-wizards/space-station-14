using Content.Server.Chemistry.EntitySystems;
using Content.Server.Fluids.Components;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;

namespace Content.Server.Fluids.EntitySystems
{
    [UsedImplicitly]
    public sealed class EvaporationSystem : EntitySystem
    {
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
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

                if (evaporationComponent.EvaporationToggle)
                {
                    _solutionContainerSystem.SplitSolution(uid, solution,
                        FixedPoint2.Min(FixedPoint2.New(1), solution.Volume)); // removes 1 unit, or solution current volume, whichever is lower.
                }

                evaporationComponent.EvaporationToggle =
                    solution.Volume > evaporationComponent.LowerLimit
                    && solution.Volume < evaporationComponent.UpperLimit;
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
