using System;
using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.Audio;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;

namespace Content.Server.GameObjects.Components.Damage
{
    [RegisterComponent]
    public class DamageOnHighSpeedImpactComponent : Component, IStartCollide
    {
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public override string Name => "DamageOnHighSpeedImpact";

        [DataField("damage")]
        public DamageType Damage { get; set; } = DamageType.Blunt;
        [DataField("minimumSpeed")]
        public float MinimumSpeed { get; set; } = 20f;
        [DataField("baseDamage")]
        public int BaseDamage { get; set; } = 5;
        [DataField("factor")]
        public float Factor { get; set; } = 1f;
        [DataField("soundHit")]
        public string SoundHit { get; set; } = "";
        [DataField("stunChance")]
        public float StunChance { get; set; } = 0.25f;
        [DataField("stunMinimumDamage")]
        public int StunMinimumDamage { get; set; } = 10;
        [DataField("stunSeconds")]
        public float StunSeconds { get; set; } = 1f;
        [DataField("damageCooldown")]
        public float DamageCooldown { get; set; } = 2f;
        private TimeSpan _lastHit = TimeSpan.Zero;

        void IStartCollide.CollideWith(Fixture ourFixture, Fixture otherFixture, in Manifold manifold)
        {
            if (!Owner.TryGetComponent(out IDamageableComponent? damageable)) return;

            var speed = ourFixture.Body.LinearVelocity.Length;

            if (speed < MinimumSpeed) return;

            if (!string.IsNullOrEmpty(SoundHit))
                SoundSystem.Play(Filter.Pvs(otherFixture.Body.Owner), SoundHit, otherFixture.Body.Owner, AudioHelpers.WithVariation(0.125f).WithVolume(-0.125f));

            if ((_gameTiming.CurTime - _lastHit).TotalSeconds < DamageCooldown)
                return;

            _lastHit = _gameTiming.CurTime;

            var damage = (int) (BaseDamage * (speed / MinimumSpeed) * Factor);

            if (Owner.TryGetComponent(out StunnableComponent? stun) && _robustRandom.Prob(StunChance))
                stun.Stun(StunSeconds);

            damageable.ChangeDamage(Damage, damage, false, otherFixture.Body.Owner);
        }
    }
}
