using Content.Shared.Chemistry;
using Robust.Shared.GameObjects;

namespace Content.Shared.GameObjects.Components.Body.Networks
{
    public abstract class SharedBloodstreamComponent : Component
    {
        /// <summary>
        ///     Attempt to transfer provided solution to internal solution.
        ///     Only supports complete transfers
        /// </summary>
        /// <param name="solution">Solution to be transferred</param>
        /// <returns>Whether or not transfer was a success</returns>
        public abstract bool TryTransferSolution(Solution solution);
    }
}
