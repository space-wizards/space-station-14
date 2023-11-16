using Content.Shared.Damage;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.Damage.Prototypes;

namespace Content.Server.Destructible.Thresholds.Triggers
{
    /// <summary>
    ///     A trigger that will activate when the amount of damage received
    ///     of the specified class is above the specified threshold.
    /// </summary>
    [Serializable]
    [DataDefinition]
    public sealed partial class DamageGroupTrigger : IThresholdTrigger
    {
        [DataField("damageGroup", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<DamageGroupPrototype>))]
        public string DamageGroup { get; set; } = default!;

        /// <summary>
        ///     The amount of damage at which this threshold will trigger.
        /// </summary>
        [DataField("damage", required: true)]
        public int Damage { get; set; } = default!;

        public bool Reached(DamageableComponent damageable, DestructibleSystem system)
        {
            return damageable.DamagePerGroup[DamageGroup] >= Damage;
        }
    }
}
