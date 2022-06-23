using Content.Shared.Damage;

namespace Content.Server.Abilities.Boxer
{
    /// <summary>
    /// Added to the boxer on spawn.
    /// </summary>
    [RegisterComponent]
    public sealed class BoxerComponent : Component
    {
        [DataField("paralyzeChanceNoSlowdown")]
        public float ParalyzeChanceNoSlowdown { get; set; } = 0.2f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("paralyzeChanceWithSlowdown")]
        public float ParalyzeChanceWithSlowdown { get; set; } = 0.5f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("paralyzeTime")]
        public float ParalyzeTime { get; set; } = 5f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("slowdownTime")]
        public float SlowdownTime { get; set; } = 3f;

        [DataField("modifiers", required: true)]
        public DamageModifierSet UnarmedModifiers = default!;

        [DataField("rangeBonus")]
        public float RangeBonus = 1.5f;
    }
}
