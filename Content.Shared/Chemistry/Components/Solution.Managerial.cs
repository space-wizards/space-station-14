using Content.Shared.FixedPoint;

namespace Content.Shared.Chemistry.Components
{
    public sealed partial class Solution
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
        public FixedPoint2 AvailableVolume => MaxVolume == null ? FixedPoint2.MaxValue : MaxVolume.Value - CurrentVolume;

        /// <summary>
        ///     Checks if a solution can fit into the container.
        /// </summary>
        /// <param name="solution">The solution that is trying to be added.</param>
        /// <returns>If the solution can be fully added.</returns>
        public bool CanAddSolution(Solution solution)
        {
            return solution.CurrentVolume <= AvailableVolume;
        }

        [DataField("maxSpillRefill")]
        public FixedPoint2 MaxSpillRefill { get; set; }

        /// <summary>
        ///     Max volume. If null, there is no limit. If zero, maximum will be inferred from initial volume.
        /// </summary>
        [DataField("maxVol")]
        [ViewVariables(VVAccess.ReadWrite)]
        public FixedPoint2? MaxVolume { get; set; } = FixedPoint2.Zero;

        [DataField("heatCapacity")]
        public float HeatCapacity { get; private set; }

        /// <summary>
        ///     The total thermal energy of the reagents in the solution.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float ThermalEnergy
        {
            get => Temperature * HeatCapacity;
            set
            {
                Temperature = HeatCapacity == 0 ? 0 : value / HeatCapacity;
            }
        }
    }
}
