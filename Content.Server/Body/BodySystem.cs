using Content.Server.GameTicking;
using Content.Server.Mind.Components;
using Content.Shared.MobState;
using Content.Shared.MobState.State;
using Content.Shared.Movement.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Content.Server.Body
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
            if (EntityManager.TryGetComponent<IMobStateComponent>(uid, out var mobState) &&
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
    }
}
