using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chemistry
{
    [UsedImplicitly]
    public partial class ChemistrySystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public void ReactionEntity(EntityUid uid, ReactionMethod method, Solution solution)
        {
            foreach (var (id, quantity) in solution)
            {
                ReactionEntity(uid, method, id, quantity, solution);
            }
        }

        public void ReactionEntity(EntityUid uid, ReactionMethod method, string reagentId, FixedPoint2 reactVolume, Solution? source)
        {
            // We throw if the reagent specified doesn't exist.
            ReactionEntity(uid, method, _prototypeManager.Index<ReagentPrototype>(reagentId), reactVolume, source);
        }

        public void ReactionEntity(EntityUid uid, ReactionMethod method, ReagentPrototype reagent,
            FixedPoint2 reactVolume, Solution? source)
        {
            if (!EntityManager.TryGetComponent(uid, out ReactiveComponent? reactive))
                return;

            // If we have a source solution, use the reagent quantity we have left. Otherwise, use the reaction volume specified.
            var args = new ReagentEffectArgs(uid, null, source, reagent,
                source?.GetReagentQuantity(reagent.ID) ?? reactVolume, EntityManager, method);

            foreach (var entry in reactive.Reactions)
            {
                if (!entry.Methods.Contains(method))
                    continue;

                if (entry.Reagents != null && !entry.Reagents.Contains(reagent.ID))
                    continue;

                foreach (var effect in entry.Effects)
                {
                    bool failed = false;
                    foreach (var cond in effect.Conditions ?? new ReagentEffectCondition[] { })
                    {
                        if (!cond.Condition(args))
                            failed = true;
                    }

                    if (failed)
                        continue;

                    effect.Metabolize(args);

                    // Make sure we still have enough reagent to go...
                    if (source != null && !source.ContainsReagent(reagent.ID))
                        break;
                }
            }
        }
    }
}
