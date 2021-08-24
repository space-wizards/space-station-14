using System;
using Content.Shared.Damage;
using Content.Shared.Sound;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Damage.Components
{
    /// <summary>
    /// Should the entity take damage / be stunned if colliding at a speed above MinimumSpeed?
    /// </summary>
    [RegisterComponent]
    internal sealed class DamageOnHighSpeedImpactComponent : Component
    {
        public override string Name => "DamageOnHighSpeedImpact";

<<<<<<< HEAD
<<<<<<< refs/remotes/origin/master
        [DataField("damage")]
        public DamageType Damage { get; set; } = DamageType.Blunt;
=======
>>>>>>> Refactor damageablecomponent update (#4406)
=======
>>>>>>> refactor-damageablecomponent
        [DataField("minimumSpeed")]
        public float MinimumSpeed { get; set; } = 20f;
        [DataField("baseDamage")]
        public int BaseDamage { get; set; } = 5;
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

        // TODO PROTOTYPE Replace this datafield variable with prototype references, once they are supported.
        // Also remove Initialize override, if no longer needed.
        [DataField("damageType")]
        private readonly string _damageTypeID = "Blunt";
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageTypePrototype DamageType = default!;
        protected override void Initialize()
        {
            base.Initialize();
            DamageType = IoCManager.Resolve<IPrototypeManager>().Index<DamageTypePrototype>(_damageTypeID);
        }
    }
}
