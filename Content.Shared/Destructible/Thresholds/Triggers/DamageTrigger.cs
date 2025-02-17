using Content.Shared.Damage;

namespace Content.Shared.Destructible.Thresholds.Triggers
{
    /// <summary>
    ///     A trigger that will activate when the amount of damage received
    ///     is above the specified threshold.
    /// </summary>
    [Serializable]
    [DataDefinition]
    public sealed partial class DamageTrigger : IThresholdTrigger
    {
        /// <summary>
        ///     The amount of damage at which this threshold will trigger.
        /// </summary>
        [DataField(required: true)]
        public int Damage = default!;

        public bool Reached(DamageableComponent damageable, EntityManager entManager)
        {
            return damageable.TotalDamage >= Damage;
        }
    }
}
