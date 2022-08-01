using Content.Shared.Damage;

namespace Content.Server.RevolutionFlag.Components
{
    [RegisterComponent, Access(typeof(FlagSystem))]
    public sealed class FlagComponent : Component
    {
        [DataField("accumulator")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float accumulator { get; set; } = 1.0f;

        [DataField("range")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float range { get; set; } = 2.5f;

        [DataField("modifiers", required: true)]
        public DamageModifierSet Modifiers = default!;

        [ViewVariables(VVAccess.ReadOnly)]
        public bool active = false;
    }
}