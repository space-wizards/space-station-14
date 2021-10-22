using Content.Server.GameTicking;
using Content.Server.Mind.Components;
using Content.Shared.MobState;
using Content.Shared.MobState.State;
using Content.Shared.Movement.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Body
{
    public sealed class BodySystem : EntitySystem
    {
        [Dependency] private readonly GameTicker _ticker = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<BodyComponent, RelayMoveInputEvent>(OnRelayMoveInput);
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
