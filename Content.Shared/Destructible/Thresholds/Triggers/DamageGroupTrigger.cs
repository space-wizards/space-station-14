using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Destructible.Thresholds.Triggers
{
    /// <summary>
    ///     A trigger that will activate when the amount of damage received
    ///     of the specified class is above the specified threshold.
    /// </summary>
    [Serializable]
    [DataDefinition]
    public sealed partial class DamageGroupTrigger : IThresholdTrigger
    {
        [DataField(required: true)]
        public ProtoId<DamageGroupPrototype> DamageGroup;

        /// <summary>
        ///     The amount of damage at which this threshold will trigger.
        /// </summary>
        [DataField(required: true)]
        public int Damage;

        public bool Reached(DamageableComponent damageable, EntityManager entManager)
        {
            return damageable.DamagePerGroup[DamageGroup] >= Damage;
        }
    }
}
