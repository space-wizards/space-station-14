using System;
using Content.Shared.Damage;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Damage.Components
{
    /// <summary>
    /// Should the entity take damage / be stunned if colliding at a speed above MinimumSpeed?
    /// </summary>
    [RegisterComponent]
    internal sealed class DamageOnHighSpeedImpactComponent : Component
    {
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

        internal TimeSpan LastHit = TimeSpan.Zero;
    }
}
