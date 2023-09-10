using Content.Shared.Damage;

namespace Content.Server.Bed.Components
{
    [RegisterComponent]
    public sealed partial class HealOnBuckleComponent : Component
    {
        [DataField("damage", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = default!;

        [DataField("healTime", required: false)]
        [ViewVariables(VVAccess.ReadWrite)]
        public float HealTime = 1f; // How often the bed applies the damage

        [DataField("sleepMultiplier")]
        public float SleepMultiplier = 3f;

        public TimeSpan NextHealTime = TimeSpan.Zero; //Next heal

        [DataField("sleepAction")] public EntityUid? SleepAction;
    }
}
