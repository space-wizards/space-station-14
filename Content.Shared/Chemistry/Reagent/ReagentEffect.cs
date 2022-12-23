using System.Text.Json.Serialization;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Shared.Random;

namespace Content.Shared.Chemistry.Reagent
{
    /// <summary>
    ///     Reagent effects describe behavior that occurs when a reagent is ingested and metabolized by some
    ///     organ. They only trigger when all of <see cref="Conditions"/> are satisfied.
    /// </summary>
    [ImplicitDataDefinitionForInheritors]
    public abstract class ReagentEffect
    {
        [JsonPropertyName("id")] private protected string _id => this.GetType().Name;
        /// <summary>
        ///     The list of conditions required for the effect to activate. Not required.
        /// </summary>
        [JsonPropertyName("conditions")]
        [DataField("conditions")]
        public ReagentEffectCondition[]? Conditions;

        /// <summary>
        ///     What's the chance, from 0 to 1, that this effect will occur?
        /// </summary>
        [JsonPropertyName("probability")]
        [DataField("probability")]
        public float Probability = 1.0f;

        [JsonIgnore]
        [DataField("logImpact")]
        public virtual LogImpact LogImpact { get; } = LogImpact.Low;

        /// <summary>
        ///     Should this reagent effect log at all?
        /// </summary>
        [JsonIgnore]
        [DataField("shouldLog")]
        public virtual bool ShouldLog { get; } = false;

        public abstract void Effect(ref ReagentEffectArgs args);
    }

    public static class ReagentEffectExt
    {
        public static bool ShouldApply(this ReagentEffect effect, ref ReagentEffectArgs args,
            IRobustRandom? random = null)
        {
            if (random == null)
                random = IoCManager.Resolve<IRobustRandom>();

            if (effect.Probability < 1.0f && !random.Prob(effect.Probability))
                return false;

            if (effect.Conditions != null)
            {
                foreach (var cond in effect.Conditions)
                {
                    if (!cond.Condition(ref args))
                        return false;
                }
            }

            return true;
        }
    }

    public enum ReactionMethod
    {
        None = 0,
        Reaction = (1<<0),
        Touch = (1<<1),
        Injection = (1<<2),
        Ingestion = (1<<3),
    }

    public readonly record struct ReagentEffectArgs(
        EntityUid SolutionEntity,
        EntityUid? OrganEntity,
        Solution? Source,
        ReagentPrototype? Reagent,
        ReactionSpecification? Reaction,
        FixedPoint2 Quantity,
        TimeSpan Time,
        IEntityManager EntityManager,
        ReactionMethod? Method,
        float Scale
    );
}
