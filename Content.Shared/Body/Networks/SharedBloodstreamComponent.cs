#nullable enable
using Content.Shared.Chemistry.Solution;
using Robust.Shared.GameObjects;

namespace Content.Shared.Body.Networks
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
    }
}
