using System.Collections.Generic;
using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Chemistry.Reagent
{
    /// <summary>
    ///     Reagent effects describe behavior that occurs when a reagent is ingested and metabolized by some
    ///     organ. They only trigger when all of <see cref="Conditions"/> are satisfied.
    /// </summary>
    [ImplicitDataDefinitionForInheritors]
    [MeansImplicitUse]
    public abstract class ReagentEffect
    {
        /// <summary>
        ///     The list of conditions required for the effect to activate. Not required.
        /// </summary>
        [DataField("conditions")]
        public ReagentEffectCondition[]? Conditions;

        /// <summary>
        ///     What's the chance, from 0 to 1, that this effect will occur?
        /// </summary>
        [DataField("probability")]
        public float Probability = 1.0f;

        [DataField("logImpact")]
        public virtual LogImpact LogImpact { get; } = LogImpact.Low;

        /// <summary>
        ///     Should this reagent effect log at all?
        /// </summary>
        [DataField("shouldLog")]
        public virtual bool ShouldLog { get; } = false;

        public abstract void Effect(ReagentEffectArgs args);
    }

    public static class ReagentEffectExt
    {
        public static bool ShouldApply(this ReagentEffect effect, ReagentEffectArgs args,
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
                    if (!cond.Condition(args))
                        return false;
                }
            }

            return true;
        }
    }

    public enum ReactionMethod
    {
        Touch,
        Injection,
        Ingestion,
    }

    public readonly record struct ReagentEffectArgs(
        EntityUid SolutionEntity,
        EntityUid? OrganEntity,
        Solution? Source,
        ReagentPrototype Reagent,
        FixedPoint2 Quantity,
        IEntityManager EntityManager,
        ReactionMethod? Method
    );
}
