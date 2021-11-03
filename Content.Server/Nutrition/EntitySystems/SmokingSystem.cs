using System.Collections.Generic;
using System.Linq;
using Content.Server.Body.Circulatory;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Nutrition.Components;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Smoking;
using Content.Shared.Temperature;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Nutrition.EntitySystems
{
    public partial class SmokingSystem : EntitySystem
    {
        [Dependency] private readonly ChemistrySystem _chemistrySystem = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;

        private const float UpdateTimer = 3f;

        private float _timer = 0f;

        /// <summary>
        ///     We keep a list of active smokables, because iterating all existing smokables would be dumb.
        /// </summary>
        private readonly HashSet<EntityUid> _active = new();

        public override void Initialize()
        {
            SubscribeLocalEvent<SmokableComponent, IsHotEvent>(OnSmokableIsHotEvent);
            SubscribeLocalEvent<SmokableComponent, ComponentShutdown>(OnSmokableShutdownEvent);

            InitializeCigars();
        }

        public void SetSmokableState(EntityUid uid, SmokableState state, SmokableComponent? smokable = null, AppearanceComponent? appearance = null)
        {
            if (!Resolve(uid, ref smokable, ref appearance))
                return;

            smokable.State = state;
            appearance.SetData(SmokingVisuals.Smoking, state);

            if (state == SmokableState.Lit)
                _active.Add(uid);
            else
                _active.Remove(uid);
        }

        private void OnSmokableIsHotEvent(EntityUid uid, SmokableComponent component, IsHotEvent args)
        {
            args.IsHot = component.State == SmokableState.Lit;
        }

        private void OnSmokableShutdownEvent(EntityUid uid, SmokableComponent component, ComponentShutdown args)
        {
            _active.Remove(uid);
        }

        public override void Update(float frameTime)
        {
            _timer += frameTime;

            if (_timer < UpdateTimer)
                return;

            foreach (var uid in _active.ToArray())
            {
                if (!EntityManager.TryGetComponent(uid, out SmokableComponent? smokable))
                {
                    _active.Remove(uid);
                    continue;
                }

                if (!_solutionContainerSystem.TryGetSolution(uid, smokable.Solution, out var solution))
                {
                    _active.Remove(uid);
                    continue;
                }

                var inhaledSolution = _solutionContainerSystem.SplitSolution(uid, solution, smokable.InhaleAmount * _timer);

                if (solution.TotalVolume == FixedPoint2.Zero)
                {
                    RaiseLocalEvent(uid, new SmokableSolutionEmptyEvent());
                }

                if (inhaledSolution.TotalVolume == FixedPoint2.Zero)
                    continue;

                // This is awful. I hate this so much.
                // TODO: Please, someone refactor containers and free me from this bullshit.
                if (!smokable.Owner.TryGetContainerMan(out var containerManager) ||
                    !containerManager.Owner.TryGetComponent(out BloodstreamComponent? bloodstream))
                    continue;

                _chemistrySystem.ReactionEntity(containerManager.Owner, ReactionMethod.Ingestion, inhaledSolution);
                bloodstream.TryTransferSolution(inhaledSolution);
            }

            _timer -= UpdateTimer;
        }
    }

    /// <summary>
    ///     Directed event raised when the smokable solution is empty.
    /// </summary>
    public class SmokableSolutionEmptyEvent : EntityEventArgs
    {
    }
}
