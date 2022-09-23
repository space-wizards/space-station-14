using Content.Shared.Damage;

namespace Content.Server.Armor
{
    [RegisterComponent]
    public sealed class ArmorComponent : Component
    {
        [DataField("modifiers", required: true)]
        public DamageModifierSet Modifiers = default!;

        /// <summary>
        ///     The examine group used for grouping together examine details.
        /// </summary>
        [DataField("examineGroup")] public string ExamineGroup = "worn-stats";

        [DataField("examinePriorityCoefficient")] public int ExaminePriorityCoefficient = 4;
        [DataField("examinePriorityFlat")] public int ExaminePriorityFlat = 5;
    }
}
