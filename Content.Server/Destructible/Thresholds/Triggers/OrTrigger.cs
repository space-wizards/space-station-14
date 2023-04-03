using Content.Shared.Damage;

namespace Content.Server.Destructible.Thresholds.Triggers
{
    /// <summary>
    ///     A trigger that will activate when any of its triggers have activated.
    /// </summary>
    [Serializable]
    [DataDefinition]
    public sealed class OrTrigger : IThresholdTrigger
    {
        [DataField("triggers")]
        public List<IThresholdTrigger> Triggers { get; } = new();

        public bool Reached(DamageableComponent damageable, DestructibleSystem system)
        {
            foreach (var trigger in Triggers)
            {
                if (trigger.Reached(damageable, system))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
