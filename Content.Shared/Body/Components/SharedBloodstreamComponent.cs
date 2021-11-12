using Content.Shared.Chemistry.Components;
using Robust.Shared.GameObjects;

namespace Content.Shared.Body.Components
{
    public abstract class SharedBloodstreamComponent : Component
    {
        /// <summary>
        ///     Attempts to transfer the provided solution to an internal solution.
        ///     Only supports complete transfers.
        /// </summary>
        /// <param name="solution">The solution to be transferred.</param>
        /// <returns>Whether or not transfer was successful.</returns>
        public abstract bool TryTransferSolution(Solution solution);

        public const string DefaultSolutionName = "bloodstream";
    }
}
