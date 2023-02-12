using System.Linq;
using System.Text.Json.Serialization;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Localizations;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffects
{
    /// <summary>
    /// Default metabolism for medicine reagents.
    /// </summary>
    [UsedImplicitly]
    public sealed class HealthChange : ReagentEffect
    {
        /// <summary>
        /// Damage to apply every metabolism cycle. Damage Ignores resistances.
        /// </summary>
        [JsonPropertyName("damage")]
        [DataField("damage", required: true)]
        public DamageSpecifier Damage = default!;

        /// <summary>
        ///     Should this effect scale the damage by the amount of chemical in the solution?
        ///     Useful for touch reactions, like styptic powder or acid.
        /// </summary>
        [JsonPropertyName("scaleByQuantity")]
        [DataField("scaleByQuantity")]
        public bool ScaleByQuantity = false;

        [DataField("ignoreResistances")]
        [JsonPropertyName("ignoreResistances")]
        public bool IgnoreResistances = true;

        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        {
            var damages = new List<string>();
            var heals = false;
            var deals = false;

            // TODO: This should be smarter. Namely, not showing a damage type as being in a group unless every damage type in the group is present and equal in value.
            foreach (var (kind, amount) in Damage.GetDamagePerGroup())
            {
                var sign = MathF.Sign(amount.Float());

                if (sign < 0)
                    heals = true;
                if (sign > 0)
                    deals = true;

                damages.Add(
                    Loc.GetString("health-change-display",
                        ("kind", kind),
                        ("amount", MathF.Abs(amount.Float())),
                        ("deltasign", sign)
                    ));
            }

            var healsordeals = heals ? (deals ? "both" : "heals") : (deals ? "deals" : "none");

            return Loc.GetString("reagent-effect-guidebook-health-change",
                ("chance", Probability),
                ("changes", ContentLocalizationManager.FormatList(damages)),
                ("healsordeals", healsordeals));
        }

        public override void Effect(ReagentEffectArgs args)
        {
            var scale = ScaleByQuantity ? args.Quantity : FixedPoint2.New(1);
            scale *= args.Scale;

            EntitySystem.Get<DamageableSystem>().TryChangeDamage(args.SolutionEntity, Damage * scale, IgnoreResistances);
        }
    }
}
