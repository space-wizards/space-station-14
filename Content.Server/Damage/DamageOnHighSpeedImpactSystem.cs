using Content.Server.Damage.Components;
using Content.Server.Stunnable.Components;
using Content.Shared.Audio;
using Content.Shared.Damage.Components;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Damage
{
    [UsedImplicitly]
    internal sealed class DamageOnHighSpeedImpactSystem: EntitySystem
    {
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DamageOnHighSpeedImpactComponent, StartCollideEvent>(HandleCollide);
        }

        private void HandleCollide(EntityUid uid, DamageOnHighSpeedImpactComponent component, StartCollideEvent args)
        {
            if (!ComponentManager.TryGetComponent(uid, out IDamageableComponent? damageable)) return;

            var otherBody = args.OtherFixture.Body.Owner;
            var speed = args.OurFixture.Body.LinearVelocity.Length;

            if (speed < component.MinimumSpeed) return;

            if (!string.IsNullOrEmpty(component.SoundHit))
                SoundSystem.Play(Filter.Pvs(otherBody), component.SoundHit, otherBody, AudioHelpers.WithVariation(0.125f).WithVolume(-0.125f));

            if ((_gameTiming.CurTime - component.LastHit).TotalSeconds < component.DamageCooldown)
                return;

            component.LastHit = _gameTiming.CurTime;

            var damage = (int) (component.BaseDamage * (speed / component.MinimumSpeed) * component.Factor);

            if (ComponentManager.TryGetComponent(uid, out StunnableComponent? stun) && _robustRandom.Prob(component.StunChance))
                stun.Stun(component.StunSeconds);

            damageable.ChangeDamage(component.Damage, damage, false, args.OtherFixture.Body.Owner);
        }
    }
}
