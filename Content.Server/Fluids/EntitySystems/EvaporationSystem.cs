using Content.Server.Chemistry.EntitySystems;
using Content.Server.Fluids.Components;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Shared.Timing;

namespace Content.Server.Fluids.EntitySystems
{
    [UsedImplicitly]
    public sealed class EvaporationSystem : EntitySystem
    {
        [Dependency] private readonly TimedEventSystem _timedEventSystem = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
        private const string EvaporationUpdateKey = nameof(EvaporationUpdateKey);

        public override void Initialize()
        {
            SubscribeLocalEvent<EvaporationComponent, ComponentInit>(OnEvaporateInit);
            SubscribeLocalEvent<EvaporationComponent, ComponentTimedEvent>(OnEvaporated);
        }
        private void OnEvaporateInit(EntityUid uid, EvaporationComponent component, ComponentInit args)
        {
            EvaporationUpdate(uid, component);
        }

        private void OnEvaporated(EntityUid uid, EvaporationComponent component, ComponentTimedEvent args)
        {
            EvaporationUpdate(uid, component);
        }

        private void EvaporationUpdate(EntityUid uid, EvaporationComponent component)
        {
            if (IsEvaporated(uid, component))
            {
                EntityManager.QueueDeleteEntity(uid);
            }
            else
            {
                _timedEventSystem.AddTimedEvent(component, TimeSpan.FromSeconds(component.EvaporateTime), EvaporationUpdateKey);
            }
        }

        private bool IsEvaporated(EntityUid uid, EvaporationComponent component)
        {
            if (!_solutionContainerSystem.TryGetSolution(uid, component.SolutionName, out var solution))
            {
                return true;
            }

            if (component.EvaporationToggle)
            {
                _solutionContainerSystem.SplitSolution(uid, solution,
                    FixedPoint2.Min(FixedPoint2.New(1), solution.CurrentVolume)); // removes 1 unit, or solution current volume, whichever is lower.
            }

            if (solution.CurrentVolume <= 0)
            {
                return true;
            }

            // If the puddle is too big or too small to evaporate, evaporation is turned off
            component.EvaporationToggle =
                solution.CurrentVolume > component.LowerLimit ||
                solution.CurrentVolume < component.UpperLimit;

            return false;
        }
    }
}
