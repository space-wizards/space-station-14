using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Components;
using Content.Shared.SCP.ConcreteSlab;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client.SCP.ConcreteSlab
{
    public sealed class SCP173System : SharedSCP173System
    {
        [Dependency] private readonly IGameTiming _timing = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SCP173Component, AttackAttemptEvent>(OnTryAttack);
            SubscribeLocalEvent<SCP173Component, OnLookStateChangedEvent>(OnLookStateChanged);
        }
        private void OnLookStateChanged(EntityUid uid, SCP173Component component, OnLookStateChangedEvent args)
        {
            if (args.IsLookedAt)
            {
                if (TryComp<InputMoverComponent>(uid, out var input)) input.CanMove = false;
            }
            if (TryComp<SpriteComponent>(uid, out var sprite)) sprite.NoRotation = args.IsLookedAt;
        }
        private void OnTryAttack(EntityUid uid, SCP173Component component, AttackAttemptEvent args)
        {
            if (!_timing.IsFirstTimePredicted) return;
            var target = args.Target.GetValueOrDefault();
            if (!target.IsValid()) return;
            if (!CanAttack(uid, target, component)) { args.Cancel(); return; }
        }
    }
}
