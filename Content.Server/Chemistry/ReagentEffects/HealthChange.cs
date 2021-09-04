using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Solution;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Content.Shared.Damage;
using Robust.Shared.Serialization;

namespace Content.Server.Chemistry.ReagentEffects
{
    /// <summary>
    /// Default metabolism for medicine reagents. Raises a <see cref="TryChangeDamageEvent"/> every metabolism cycle while this reagent is in the bloodstream.
    /// </summary>
    public class HealthChange : ReagentEffect, ISerializationHooks
    {
        /// <summary>
        /// Damage to apply every metabolism cycle. Damage Ignores resistances.
        /// </summary>
        [DataField("damage", required: true)]
        public DamageSpecifier Damage = default!;

        public override void Metabolize(IEntity solutionEntity, Solution.ReagentQuantity amount)
        {
            EntitySystem.Get<DamageableSystem>().TryChangeDamage(solutionEntity, Damage, true);
        }
    }
}
