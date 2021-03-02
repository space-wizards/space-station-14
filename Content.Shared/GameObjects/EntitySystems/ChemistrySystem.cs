#nullable enable
using System;
using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

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
    }
}
