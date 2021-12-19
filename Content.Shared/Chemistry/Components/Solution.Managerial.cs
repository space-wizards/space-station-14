using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
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

        /// <summary>
        ///     The total heat capacity of all reagents in the solution.
        /// </summary>
        [ViewVariables]
        public double HeatCapacity => GetHeatCapacity();

        /// <summary>
        ///     The average specific heat of all reagents in the solution.
        /// </summary>
        [ViewVariables]
        public double SpecificHeat => HeatCapacity / (double) TotalVolume;

        /// <summary>
        ///     The total thermal energy of the reagents in the solution.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public double ThermalEnergy {
            get { return Temperature * HeatCapacity; }
            set { Temperature = ((HeatCapacity == 0.0d) ? 0.0d : (value / HeatCapacity)); }
        }

        /// <summary>
        ///     Returns the total heat capacity of the reagents in this solution.
        /// </summary>
        /// <returns>The total heat capacity of the reagents in this solution.</returns>
        private double GetHeatCapacity()
        {
            var heatCapacity = 0.0d;
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            foreach(var reagent in Contents)
            {
                if (!prototypeManager.TryIndex(reagent.ReagentId, out ReagentPrototype? proto))
                    proto = new ReagentPrototype();

                heatCapacity += (double) reagent.Quantity * proto.SpecificHeat;
            }

            return heatCapacity;
        }
    }
}
