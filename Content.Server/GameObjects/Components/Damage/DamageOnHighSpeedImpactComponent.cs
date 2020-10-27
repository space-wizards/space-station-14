using System;
using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.Audio;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Damage
{
    [RegisterComponent]
    public class DamageOnHighSpeedImpactComponent : Component, ICollideBehavior
    {
        [Dependency] private IRobustRandom _robustRandom = default!;
        [Dependency] private IGameTiming _gameTiming = default!;

        public override string Name => "DamageOnHighSpeedImpact";

        public DamageType Damage { get; set; } = DamageType.Blunt;
        public float MinimumSpeed { get; set; } = 20f;
        public int BaseDamage { get; set; } = 5;
        public float Factor { get; set; } = 0.75f;
        public string SoundHit { get; set; } = "";
        public float StunChance { get; set; } = 0.25f;
        public int StunMinimumDamage { get; set; } = 10;
        public float StunSeconds { get; set; } = 1f;
        public float DamageCooldown { get; set; } = 2f;
        private TimeSpan _lastHit = TimeSpan.Zero;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => Damage, "damage", DamageType.Blunt);
            serializer.DataField(this, x => MinimumSpeed, "minimumSpeed", 20f);
            serializer.DataField(this, x => BaseDamage, "baseDamage", 5);
            serializer.DataField(this, x => Factor, "factor", 1f);
            serializer.DataField(this, x => SoundHit, "soundHit", "");
            serializer.DataField(this, x => StunChance, "stunChance", 0.25f);
            serializer.DataField(this, x => StunSeconds, "stunSeconds", 1f);
            serializer.DataField(this, x => DamageCooldown, "damageCooldown", 2f);
            serializer.DataField(this, x => StunMinimumDamage, "stunMinimumDamage", 10);
        }

        public void CollideWith(IEntity collidedWith)
        {
            if (!Owner.TryGetComponent(out IPhysicsComponent physics) || !Owner.TryGetComponent(out IDamageableComponent damageable)) return;

            var speed = physics.LinearVelocity.Length;

            if (speed < MinimumSpeed) return;

            if(!string.IsNullOrEmpty(SoundHit))
                EntitySystem.Get<AudioSystem>().PlayFromEntity(SoundHit, collidedWith, AudioHelpers.WithVariation(0.125f).WithVolume(-0.125f));

            if ((_gameTiming.CurTime - _lastHit).TotalSeconds < DamageCooldown)
                return;

            _lastHit = _gameTiming.CurTime;

            var damage = (int) (BaseDamage * (speed / MinimumSpeed) * Factor);

            if (Owner.TryGetComponent(out StunnableComponent stun) && _robustRandom.Prob(StunChance))
                stun.Stun(StunSeconds);

            damageable.ChangeDamage(Damage, damage, false, collidedWith);
        }
    }
}
