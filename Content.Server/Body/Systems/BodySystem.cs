using System.Diagnostics.CodeAnalysis;
using Content.Server.Body.Components;
using Content.Server.GameTicking;
using Content.Server.Kitchen.Components;
using Content.Server.Mind.Components;
using Content.Shared.Body.Components;
using Content.Shared.MobState.Components;
using Content.Shared.Movement.Events;
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
            SubscribeLocalEvent<BodyComponent, MoveInputEvent>(OnRelayMoveInput);
            SubscribeLocalEvent<BodyComponent, ApplyMetabolicMultiplierEvent>(OnApplyMetabolicMultiplier);
            SubscribeLocalEvent<BodyComponent, BeingMicrowavedEvent>(OnBeingMicrowaved);
            SubscribeLocalEvent<BodyPartComponent, MapInitEvent>((_, c, _) => c.MapInitialize());
        }

        private void OnRelayMoveInput(EntityUid uid, BodyComponent component, ref MoveInputEvent args)
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

        private void OnApplyMetabolicMultiplier(EntityUid uid, BodyComponent component, ApplyMetabolicMultiplierEvent args)
        {
            foreach (var (part, _) in component.Parts)
            foreach (var mechanism in part.Mechanisms)
            {
                RaiseLocalEvent(mechanism.Owner, args, false);
            }
        }

        private void OnBeingMicrowaved(EntityUid uid, BodyComponent component, BeingMicrowavedEvent args)
        {
            if (args.Handled)
                return;

            // Don't microwave animals, kids
            Transform(uid).AttachToGridOrMap();
            component.Gib();

            args.Handled = true;
        }

        /// <summary>
        ///     Returns a list of ValueTuples of <see cref="T"/> and MechanismComponent on each mechanism
        ///     in the given body.
        /// </summary>
        /// <param name="uid">The entity to check for the component on.</param>
        /// <param name="body">The body to check for mechanisms on.</param>
        /// <typeparam name="T">The component to check for.</typeparam>
        public List<(T Comp, MechanismComponent Mech)> GetComponentsOnMechanisms<T>(EntityUid uid,
            SharedBodyComponent? body=null) where T : Component
        {
            if (!Resolve(uid, ref body))
                return new();

            var query = EntityManager.GetEntityQuery<T>();
            var list = new List<(T Comp, MechanismComponent Mech)>(3);
            foreach (var (part, _) in body.Parts)
            foreach (var mechanism in part.Mechanisms)
            {
                if (query.TryGetComponent(mechanism.Owner, out var comp))
                    list.Add((comp, mechanism));
            }

            return list;
        }

        /// <summary>
        ///     Tries to get a list of ValueTuples of <see cref="T"/> and MechanismComponent on each mechanism
        ///     in the given body.
        /// </summary>
        /// <param name="uid">The entity to check for the component on.</param>
        /// <param name="comps">The list of components.</param>
        /// <param name="body">The body to check for mechanisms on.</param>
        /// <typeparam name="T">The component to check for.</typeparam>
        /// <returns>Whether any were found.</returns>
        public bool TryGetComponentsOnMechanisms<T>(EntityUid uid,
            [NotNullWhen(true)] out List<(T Comp, MechanismComponent Mech)>? comps,
            SharedBodyComponent? body=null) where T: Component
        {
            if (!Resolve(uid, ref body))
            {
                comps = null;
                return false;
            }

            comps = GetComponentsOnMechanisms<T>(uid, body);

            if (comps.Count == 0)
            {
                comps = null;
                return false;
            }

            return true;
        }
    }
}
