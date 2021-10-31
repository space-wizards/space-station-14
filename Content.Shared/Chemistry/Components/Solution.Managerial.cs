using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.Chemistry.Components
{
    public partial class Solution
    {

        /// <summary>
        ///     If reactions will be checked for when adding reagents to the container.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("canReact")]
        public bool CanReact { get; set; } = true;

        /// <summary>
        ///     Volume needed to fill this container.
        /// </summary>
        [ViewVariables]
        public ReagentUnit AvailableVolume => MaxVolume - CurrentVolume;

        public ReagentUnit DrawAvailable => CurrentVolume;
        public ReagentUnit DrainAvailable => CurrentVolume;

        /// <summary>
        ///     Checks if a solution can fit into the container.
        /// </summary>
        /// <param name="solution">The solution that is trying to be added.</param>
        /// <returns>If the solution can be fully added.</returns>
        public bool CanAddSolution(Solution solution)
        {
            return solution.TotalVolume <= AvailableVolume;
        }

        [DataField("maxSpillRefill")]
        public ReagentUnit MaxSpillRefill { get; set; }

        /// <summary>
        /// Initially set <see cref="MaxVolume"/>. If empty will be calculated based
        /// on sum of <see cref="Contents"/> reagent units.
        /// </summary>
        [DataField("maxVol")] public ReagentUnit InitialMaxVolume; 

        [ViewVariables(VVAccess.ReadWrite)]
        public ReagentUnit MaxVolume { get; set; } = ReagentUnit.Zero;

        [ViewVariables]
        public ReagentUnit CurrentVolume => TotalVolume;
    }
}
