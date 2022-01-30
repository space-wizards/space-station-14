using System;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.ViewVariables;

namespace Content.Server.Bible.Components
{
    [RegisterComponent, ComponentProtoName("Bible")]
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
        [DataField("failChance", required:true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public float FailChance = 0.34f;

        public TimeSpan LastAttackTime;
        public TimeSpan CooldownEnd;
        public float CooldownTime { get; } = 5f;
    }
}
