using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
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
        public FixedPoint2 AvailableVolume => MaxVolume - CurrentVolume;

        public FixedPoint2 DrawAvailable => CurrentVolume;
        public FixedPoint2 DrainAvailable => CurrentVolume;

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
        public FixedPoint2 MaxSpillRefill { get; set; }

        /// <summary>
        /// Initially set <see cref="MaxVolume"/>. If empty will be calculated based
        /// on sum of <see cref="Contents"/> fixed units.
        /// </summary>
        [DataField("maxVol")] public FixedPoint2 InitialMaxVolume;

        [ViewVariables(VVAccess.ReadWrite)]
        public FixedPoint2 MaxVolume { get; set; } = FixedPoint2.Zero;

        [ViewVariables]
        public FixedPoint2 CurrentVolume => TotalVolume;
    }
}
