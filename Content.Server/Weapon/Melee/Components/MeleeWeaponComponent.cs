using Content.Shared.Damage;
using Robust.Shared.Audio;
using Content.Shared.FixedPoint;

namespace Content.Server.Weapon.Melee.Components
{
    [RegisterComponent]
    public sealed class MeleeWeaponComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("hitSound")]
        public SoundSpecifier? HitSound;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("noDamageSound")]
        public SoundSpecifier NoDamageSound { get; set; } = new SoundPathSpecifier("/Audio/Weapons/tap.ogg");

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("missSound")]
        public SoundSpecifier MissSound { get; set; } = new SoundPathSpecifier("/Audio/Weapons/punchmiss.ogg");

        [ViewVariables]
        [DataField("arcCooldownTime")]
        public float ArcCooldownTime { get; } = 1f;

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

        public TimeSpan LastAttackTime;
        public TimeSpan CooldownEnd;

        [DataField("damage", required:true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = default!;

        [DataField("bluntStaminaDamageFactor")]
        [ViewVariables(VVAccess.ReadWrite)]
        public FixedPoint2 BluntStaminaDamageFactor { get; set; } = 0.5f;
    }
}
