using Content.Shared.Damage;
using Robust.Shared.Audio;

namespace Content.Server.Bible.Components
{
    [RegisterComponent]
    public sealed class BibleComponent : Component
    {
        // Damage that will be healed on a success
        [DataField("damage", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = default!;
        // Damage that will be dealt on a failure
        [DataField("damageOnFail", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier DamageOnFail = default!;
        // Damage that will be dealt when a non-chaplain attempts to heal
        [DataField("damageOnUntrainedUse", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier DamageOnUntrainedUse = default!;

        //Chance the bible will fail to heal someone with no helmet
        [DataField("failChance")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float FailChance = 0.34f;

        public TimeSpan LastAttackTime;
        public TimeSpan CooldownEnd;
        [DataField("cooldownTime")]
        public float CooldownTime { get; } = 5f;

        [DataField("sizzleSound")]
        public SoundSpecifier SizzleSoundPath = new SoundPathSpecifier("/Audio/Effects/lightburn.ogg");
        [DataField("healSound")]
        public SoundSpecifier HealSoundPath = new  SoundPathSpecifier("/Audio/Effects/holy.ogg");

        [DataField("locPrefix")]
        public string LocPrefix = "bible";
    }
}
