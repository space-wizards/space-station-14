using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Body.Components;
using Content.Server.GameTicking;
using Content.Server.Mind.Components;
using Content.Shared.Body.Components;
using Content.Shared.MobState.Components;
using Content.Shared.Movement.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Content.Server.Body.Systems
{
    public sealed class BodySystem : EntitySystem
    {
        [Dependency] private readonly GameTicker _ticker = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<BodyComponent, RelayMoveInputEvent>(OnRelayMoveInput);
        }

        private void OnRelayMoveInput(EntityUid uid, BodyComponent component, RelayMoveInputEvent args)
        {
            if (EntityManager.TryGetComponent<MobStateComponent>(uid, out var mobState) &&
                mobState.IsDead() &&
                EntityManager.TryGetComponent<MindComponent>(uid, out var mind) &&
                mind.HasMind)
            {
                if (!mind.Mind!.TimeOfDeath.HasValue)
                {
                    mind.Mind.TimeOfDeath = _gameTiming.RealTime;
                }

                _ticker.OnGhostAttempt(mind.Mind!, true);
            }
        }

        /// <summary>
        ///     Returns a list of ValueTuples of <see cref="T"/> and SharedMechanismComponent on each mechanism
        ///     in the given body.
        /// </summary>
        /// <param name="uid">The entity to check for the component on.</param>
        /// <param name="body">The body to check for mechanisms on.</param>
        /// <typeparam name="T">The component to check for.</typeparam>
        public IEnumerable<(T Comp, SharedMechanismComponent Mech)> GetComponentsOnMechanisms<T>(EntityUid uid,
            SharedBodyComponent? body=null) where T : Component
        {
            if (!Resolve(uid, ref body))
                yield break;

            foreach (var (part, _) in body.Parts)
            foreach (var mechanism in part.Mechanisms)
            {
                if (EntityManager.TryGetComponent<T>((mechanism).Owner, out var comp))
                    yield return (comp, mechanism);
            }
        }

        /// <summary>
        ///     Tries to get a list of ValueTuples of <see cref="T"/> and SharedMechanismComponent on each mechanism
        ///     in the given body.
        /// </summary>
        /// <param name="uid">The entity to check for the component on.</param>
        /// <param name="comps">The list of components.</param>
        /// <param name="body">The body to check for mechanisms on.</param>
        /// <typeparam name="T">The component to check for.</typeparam>
        /// <returns>Whether any were found.</returns>
        public bool TryGetComponentsOnMechanisms<T>(EntityUid uid,
            [NotNullWhen(true)] out IEnumerable<(T Comp, SharedMechanismComponent Mech)>? comps,
            SharedBodyComponent? body=null) where T: Component
        {
            if (!Resolve(uid, ref body))
            {
                comps = null;
                return false;
            }

            comps = GetComponentsOnMechanisms<T>(uid, body).ToArray();

            if (!comps.Any())
            {
                comps = null;
                return false;
            }

            return true;
        }
    }
}
