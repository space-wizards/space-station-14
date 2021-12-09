using System;
using Content.Server.Alert;
using Content.Server.Stunnable.Components;
using Content.Shared.Standing;
using Content.Shared.StatusEffect;
using JetBrains.Annotations;
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
            var otherUid = args.OtherFixture.Body.Owner;

            if (EntityManager.TryGetComponent<StatusEffectsComponent>(otherUid, out var status))
            {
                ServerAlertsComponent? alerts = null;
                StandingStateComponent? standingState = null;
                AppearanceComponent? appearance = null;

                // Let the actual methods log errors for these.
                Resolve(otherUid, ref alerts, ref standingState, ref appearance, false);

                _stunSystem.TryStun(otherUid, TimeSpan.FromSeconds(component.StunAmount), true, status, alerts);

                _stunSystem.TryKnockdown(otherUid, TimeSpan.FromSeconds(component.KnockdownAmount), true,
                    status, alerts);

                _stunSystem.TrySlowdown(otherUid, TimeSpan.FromSeconds(component.SlowdownAmount), true,
                    component.WalkSpeedMultiplier, component.RunSpeedMultiplier, status, alerts);
            }
        }
    }
}
