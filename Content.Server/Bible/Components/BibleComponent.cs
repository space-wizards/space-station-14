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
    [RegisterComponent]
    public class BibleComponent : Component
    {
        public override string Name => "Bible";

        // Damage that will be healed on a success
        [DataField("damage", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = default!;
        // Damage that will be dealt on a failure
        [DataField("damageOnFail", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier DamageOnFail = default!;

        [DataField("damageOnUntrainedUse", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier DamageOnUntrainedUse = default!;

        public TimeSpan LastAttackTime;
        public TimeSpan CooldownEnd;
        public float CooldownTime { get; } = 1f;


        [DataField("damageContainer", customTypeSerializer: typeof(PrototypeIdSerializer<DamageContainerPrototype>))]
        //Maybe if we get our favorite GW copyright infringing priestmen they can heal robots, otherwise this just filters for living beings
        public string? DamageContainerID = "Biological";
    }
}
