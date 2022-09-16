using Content.Server.Damage.Components;
using Content.Server.Stunnable;
using Content.Shared.Audio;
using Content.Shared.Damage;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Damage.Systems
{
    [UsedImplicitly]
    internal sealed class DamageOnHighSpeedImpactSystem: EntitySystem
    {
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly DamageableSystem _damageableSystem = default!;
        [Dependency] private readonly StunSystem _stunSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DamageOnHighSpeedImpactComponent, StartCollideEvent>(HandleCollide);
        }

        private void HandleCollide(EntityUid uid, DamageOnHighSpeedImpactComponent component, ref StartCollideEvent args)
        {
            if (!EntityManager.HasComponent<DamageableComponent>(uid)) return;

            var otherBody = args.OtherFixture.Body.Owner;
            var speed = args.OurFixture.Body.LinearVelocity.Length;

            if (speed < component.MinimumSpeed) return;

            SoundSystem.Play(component.SoundHit.GetSound(), Filter.Pvs(otherBody), otherBody, AudioHelpers.WithVariation(0.125f).WithVolume(-0.125f));

            if ((_gameTiming.CurTime - component.LastHit).TotalSeconds < component.DamageCooldown)
                return;

            component.LastHit = _gameTiming.CurTime;

            if (_robustRandom.Prob(component.StunChance))
                _stunSystem.TryStun(uid, TimeSpan.FromSeconds(component.StunSeconds), true);

            var damageScale = (speed / component.MinimumSpeed) * component.Factor;

            _damageableSystem.TryChangeDamage(uid, component.Damage * damageScale);
        }
    }
}
