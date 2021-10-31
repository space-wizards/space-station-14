using System.Collections.Generic;
using Content.Server.Body.Components;
using Content.Server.GameTicking;
using Content.Server.Mind.Components;
using Content.Shared.Body.Components;
using Content.Shared.MobState;
using Content.Shared.Movement.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Body.EntitySystems
{
    /// <summary>
    ///     Handles everything related to bodies, their parts & their connections.
    /// </summary>
    public class BodySystem : EntitySystem
    {
        [Dependency] private readonly GameTicker _ticker = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<BodyComponent, RelayMoveInputEvent>(OnRelayMoveInput);
        }

        public IEnumerable<T> GetComponentsOnMechanisms<T>(SharedBodyComponent body)
            where T : Component
        {
            foreach (var part in body.Parts)
            {
                foreach (var mech in part.Key.Mechanisms)
                {
                    if (mech.Owner.TryGetComponent<T>(out var comp))
                    {
                        yield return comp;
                    }
                }
            }
        }

        private void OnRelayMoveInput(EntityUid uid, BodyComponent component, RelayMoveInputEvent args)
        {
            if (EntityManager.TryGetComponent<IMobStateComponent>(uid, out var mobState) &&
                mobState.IsDead() &&
                EntityManager.TryGetComponent<MindComponent>(uid, out var mind) &&
                mind.HasMind)
            {
                _ticker.OnGhostAttempt(mind.Mind!, true);
            }
        }
    }
}
