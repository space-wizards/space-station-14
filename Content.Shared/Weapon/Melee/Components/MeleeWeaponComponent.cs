using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapon.Melee.Components
{
    [RegisterComponent, NetworkedComponent]
    public sealed class MeleeWeaponComponent : Component
    {
        /// <summary>
        /// Do we only attack our clicked entity or do we do wide attacks?
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool PrecisionMode = false;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("hitSound")]
        public SoundSpecifier? HitSound;

        /// <summary>
        /// Plays if no damage is done to the target entity.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("noDamageSound")]
        public SoundSpecifier NoDamageSound { get; set; } = new SoundPathSpecifier("/Audio/Weapons/tap.ogg");

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("missSound")]
        public SoundSpecifier MissSound { get; set; } = new SoundPathSpecifier("/Audio/Weapons/punchmiss.ogg");

        [ViewVariables]
        [DataField("cooldownTime")]
        public float CooldownTime { get; } = 1f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("clickArc")]
        public string ClickArc { get; set; } = "punch";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("arc")]
        public string? Arc { get; set; } = "default";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("arcwidth")]
        public float ArcWidth { get; set; } = 90;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("range")]
        public float Range { get; set; } = 1;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("clickAttackEffect")]
        public bool ClickAttackEffect { get; set; } = true;

        // TODO: When we can predict comp changes just make this a cooldown accumulator on an active component.
        /// <summary>
        /// Next time the weapon can attack.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField("nextAttackTime")]
        public TimeSpan NextAttackTime;

        [DataField("damage", required:true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = default!;
    }
}
