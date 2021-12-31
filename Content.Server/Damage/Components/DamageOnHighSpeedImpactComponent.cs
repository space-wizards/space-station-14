using System;
using Content.Shared.Damage;
using Content.Shared.Sound;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Damage.Components
{
    /// <summary>
    /// Should the entity take damage / be stunned if colliding at a speed above MinimumSpeed?
    /// </summary>
    [RegisterComponent]
    internal sealed class DamageOnHighSpeedImpactComponent : Component
    {
        [DataField("minimumSpeed")]
        public float MinimumSpeed { get; set; } = 20f;
        [DataField("factor")]
        public float Factor { get; set; } = 1f;
        [DataField("soundHit", required: true)]
        public SoundSpecifier SoundHit { get; set; } = default!;
        [DataField("stunChance")]
        public float StunChance { get; set; } = 0.25f;
        [DataField("stunMinimumDamage")]
        public int StunMinimumDamage { get; set; } = 10;
        [DataField("stunSeconds")]
        public float StunSeconds { get; set; } = 1f;
        [DataField("damageCooldown")]
        public float DamageCooldown { get; set; } = 2f;

        internal TimeSpan LastHit = TimeSpan.Zero;

        [DataField("damage", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = default!;
    }
}
