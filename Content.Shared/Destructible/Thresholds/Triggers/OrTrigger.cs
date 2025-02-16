using Content.Shared.Damage;

namespace Content.Shared.Destructible.Thresholds.Triggers
{
    /// <summary>
    ///     A trigger that will activate when any of its triggers have activated.
    /// </summary>
    [Serializable]
    [DataDefinition]
    public sealed partial class OrTrigger : IThresholdTrigger
    {
        [DataField]
        public List<IThresholdTrigger> Triggers { get; private set; } = new();

        public bool Reached(DamageableComponent damageable, EntityManager entManager)
        {
            foreach (var trigger in Triggers)
            {
                if (trigger.Reached(damageable, entManager))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
