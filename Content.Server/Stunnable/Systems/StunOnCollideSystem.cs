using Content.Server.Stunnable.Components;
using Content.Shared.Movement.Systems;
using JetBrains.Annotations;
using Content.Shared.Throwing;
using Robust.Shared.Physics.Events;

namespace Content.Server.Stunnable
{
    [UsedImplicitly]
    internal sealed class StunOnCollideSystem : EntitySystem
    {
        [Dependency] private readonly StunSystem _stunSystem = default!;
        [Dependency] private readonly MovementModStatusSystem _movementMod = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<StunOnCollideComponent, StartCollideEvent>(HandleCollide);
            SubscribeLocalEvent<StunOnCollideComponent, ThrowDoHitEvent>(HandleThrow);
        }

        private void TryDoCollideStun(EntityUid uid, StunOnCollideComponent component, EntityUid target)
        {
            _stunSystem.TryUpdateStunDuration(target, component.StunAmount);

            _stunSystem.TryKnockdown(target, component.KnockdownAmount, component.Refresh, component.AutoStand, force: true);

            _movementMod.TryUpdateMovementSpeedModDuration(
                target,
                MovementModStatusSystem.TaserSlowdown,
                component.SlowdownAmount,
                component.WalkSpeedModifier,
                component.SprintSpeedModifier
            );
        }

        private void HandleCollide(EntityUid uid, StunOnCollideComponent component, ref StartCollideEvent args)
        {
            if (args.OurFixtureId != component.FixtureID)
                return;

            TryDoCollideStun(uid, component, args.OtherEntity);
        }

        private void HandleThrow(EntityUid uid, StunOnCollideComponent component, ThrowDoHitEvent args)
        {
            TryDoCollideStun(uid, component, args.Target);
        }
    }
}
