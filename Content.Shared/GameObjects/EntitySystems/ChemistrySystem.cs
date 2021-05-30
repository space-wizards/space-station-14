#nullable enable
using System;
using System.Linq;
using Content.Shared.Chemistry;
using Content.Shared.GameObjects.Components.Chemistry;
using Content.Shared.Interfaces.GameObjects.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Shared.GameObjects.EntitySystems
{
    /// <summary>
    /// This interface gives components behavior on whether entities solution (implying SolutionComponent is in place) is changed
    /// </summary>
    public interface ISolutionChange
    {
        /// <summary>
        /// Called when solution is mixed with some other solution, or when some part of the solution is removed
        /// </summary>
        void SolutionChanged(SolutionChangeEventArgs eventArgs);
    }

    public class SolutionChangeEventArgs : EventArgs
    {
        public IEntity Owner { get; set; } = default!;
    }

    [UsedImplicitly]
    public class ChemistrySystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public void HandleSolutionChange(IEntity owner)
        {
            var eventArgs = new SolutionChangeEventArgs
            {
                Owner = owner,
            };
            var solutionChangeArgs = owner.GetAllComponents<ISolutionChange>().ToList();

            foreach (var solutionChangeArg in solutionChangeArgs)
            {
                solutionChangeArg.SolutionChanged(eventArgs);

                if (owner.Deleted)
                    return;
            }
        }

        public void ReactionEntity(IEntity? entity, ReactionMethod method, string reagentId, ReagentUnit reactVolume, Solution? source)
        {
            // We throw if the reagent specified doesn't exist.
            ReactionEntity(entity, method, _prototypeManager.Index<ReagentPrototype>(reagentId), reactVolume, source);
        }

        public void ReactionEntity(IEntity? entity, ReactionMethod method, ReagentPrototype reagent, ReagentUnit reactVolume, Solution? source)
        {
            if (entity == null || entity.Deleted || !entity.TryGetComponent(out ReactiveComponent? reactive))
                return;

            foreach (var reaction in reactive.Reactions)
            {
                // If we have a source solution, use the reagent quantity we have left. Otherwise, use the reaction volume specified.
                reaction.React(method, entity, reagent, source?.GetReagentQuantity(reagent.ID) ?? reactVolume, source);

                // Make sure we still have enough reagent to go...
                if (source != null && !source.ContainsReagent(reagent.ID))
                    break;
            }
        }
    }
}
