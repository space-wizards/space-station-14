using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Destructible.Thresholds.Triggers
{
    /// <summary>
    ///     A trigger that will activate when the amount of damage received
    ///     of the specified type is above the specified threshold.
    /// </summary>
    [Serializable]
    [DataDefinition]
    public sealed partial class DamageTypeTrigger : IThresholdTrigger
    {
        [DataField(required: true)]
        public ProtoId<DamageTypePrototype> DamageType;

        [DataField(required: true)]
        public int Damage;

        public bool Reached(DamageableComponent damageable, EntityManager entManager)
        {
            return damageable.Damage.DamageDict.TryGetValue(DamageType, out var damageReceived) &&
                   damageReceived >= Damage;
        }
    }
}
