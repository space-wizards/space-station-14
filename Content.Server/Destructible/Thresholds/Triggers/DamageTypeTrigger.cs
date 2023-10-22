using Content.Shared.Damage;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.Damage.Prototypes;

namespace Content.Server.Destructible.Thresholds.Triggers
{
    /// <summary>
    ///     A trigger that will activate when the amount of damage received
    ///     of the specified type is above the specified threshold.
    /// </summary>
    [Serializable]
    [DataDefinition]
    public sealed partial class DamageTypeTrigger : IThresholdTrigger
    {
        [DataField("damageType", required:true, customTypeSerializer: typeof(PrototypeIdSerializer<DamageTypePrototype>))]
        public string DamageType { get; set; } = default!;

        [DataField("damage", required: true)]
        public int Damage { get; set; } = default!;

        public bool Reached(DamageableComponent damageable, DestructibleSystem system)
        {
            return damageable.Damage.DamageDict.TryGetValue(DamageType, out var damageReceived) &&
                   damageReceived >= Damage;
        }
    }
}
