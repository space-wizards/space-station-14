using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;

namespace Content.Server.Chemistry.ReagentEffects
{
    /// <summary>
    /// Default metabolism for medicine reagents.
    /// </summary>
    [UsedImplicitly]
    public class HealthChange : ReagentEffect
    {
        /// <summary>
        /// Damage to apply every metabolism cycle. Damage Ignores resistances.
        /// </summary>
        [DataField("damage", required: true)]
        public DamageSpecifier Damage = default!;

        /// <summary>
        ///     Should this effect scale the damage by the amount of chemical in the solution?
        ///     Useful for touch reactions, like styptic powder or acid.
        /// </summary>
        [DataField("scaleByQuantity")]
        public bool ScaleByQuantity = false;

        [DataField("ignoreResistances")]
        public bool IgnoreResistances = true;

        public override void Effect(ReagentEffectArgs args)
        {
            var scale = ScaleByQuantity ? args.Quantity : FixedPoint2.New(1);
            EntitySystem.Get<DamageableSystem>().TryChangeDamage(args.SolutionEntity, Damage * scale, IgnoreResistances);
        }
    }
}
