using System;
using Content.Shared.Damage;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Prototypes;
using Robust.Shared.IoC;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.Destructible.Thresholds.Triggers
{
    /// <summary>
    ///     A trigger that will activate when the amount of damage received
    ///     of the specified class is above the specified threshold.
    /// </summary>
    [Serializable]
    [DataDefinition]
    public class DamageGroupTrigger : IThresholdTrigger, ISerializationHooks
    {
        // TODO PROTOTYPE Replace this datafield variable with prototype references, once they are supported.
        [DataField("damageGroup", required: true)]
        private string _damageGroupID { get; set; } = default!;
        public DamageGroupPrototype DamageGroup = default!;
        void ISerializationHooks.AfterDeserialization()
        {
            DamageGroup = IoCManager.Resolve<IPrototypeManager>().Index<DamageGroupPrototype>(_damageGroupID);
        }

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
