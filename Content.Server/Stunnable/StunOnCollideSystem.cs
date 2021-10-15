using System;
using Content.Server.Alert;
using Content.Server.Stunnable.Components;
using Content.Shared.Alert;
using Content.Shared.Movement.Components;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics.Dynamics;

namespace Content.Server.Stunnable
{
    [UsedImplicitly]
    internal sealed class StunOnCollideSystem : EntitySystem
    {
        [Dependency] private readonly StunSystem _stunSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<StunOnCollideComponent, StartCollideEvent>(HandleCollide);
        }

        private void HandleCollide(EntityUid uid, StunOnCollideComponent component, StartCollideEvent args)
        {
            var otherUid = args.OtherFixture.Body.Owner.Uid;

            if (EntityManager.TryGetComponent(otherUid, out StunnableComponent? stunnableComponent))
            {
                ServerAlertsComponent? alerts = null;
                StandingStateComponent? standingState = null;
                AppearanceComponent? appearance = null;
                MovementSpeedModifierComponent? speedModifier = null;

                // Let the actual methods log errors for these.
                Resolve(otherUid, ref alerts, ref standingState, ref appearance, ref speedModifier, false);

                _stunSystem.Stun(otherUid, TimeSpan.FromSeconds(component.StunAmount), stunnableComponent, alerts);

                _stunSystem.Knockdown(otherUid, TimeSpan.FromSeconds(component.KnockdownAmount), stunnableComponent,
                    alerts, standingState, appearance);

                _stunSystem.Slowdown(otherUid, TimeSpan.FromSeconds(component.SlowdownAmount),
                    component.WalkSpeedMultiplier, component.RunSpeedMultiplier, stunnableComponent, speedModifier, alerts);
            }
        }
    }
}
