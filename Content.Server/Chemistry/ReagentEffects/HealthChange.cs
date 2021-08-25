using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Solution;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Content.Shared.Damage;
using Robust.Shared.Serialization;

namespace Content.Server.Chemistry.ReagentEffects
{
    /// <summary>
    /// Default metabolism for medicine reagents. Attempts to find a DamageableComponent on the target,
    /// and to update its damage values.
    /// </summary>
    public class HealthChange : ReagentEffect, ISerializationHooks
    {

        /// <summary>
        /// Damage to apply when 1u of reagent is consumed.
        /// </summary>
        [DataField("damage", required: true)]
        public DamageData Damage = default!;

        /// <summary>
        ///     Changes damage if a DamageableComponent can be found.
        /// </summary>
        public override void Metabolize(IEntity solutionEntity, Solution.ReagentQuantity amount)
        {
            solutionEntity.EntityManager.EventBus.RaiseLocalEvent(solutionEntity.Uid, new TryChangeDamageEvent(Damage, true), false);
        }
    }
}
