using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

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

        private float _heatCapacity;
        private bool _heatCapacityDirty;

        /// <summary>
        ///     Sets the total thermal energy of the reagents in the solution.
        /// </summary>
        public void SetThermalEnergy(float value, IPrototypeManager? protoMan)
        {
            IoCManager.Resolve(ref protoMan);
            var heatCap = GetHeatCapacity(protoMan);
            Temperature = heatCap == 0 ? 0 : value / heatCap;
        }

        /// <summary>
        ///     Gets total thermal energy of the reagents in the solution.
        /// </summary>
        public float GetThermalEnergy(IPrototypeManager? protoMan) => Temperature * GetHeatCapacity(protoMan);

        /// <summary>
        ///     Returns the total heat capacity of the reagents in this solution.
        /// </summary>
        /// <returns>The total heat capacity of the reagents in this solution.</returns>
        private float GetHeatCapacity(IPrototypeManager? protoMan)
        {
            if (!_heatCapacityDirty)
                return _heatCapacity;

            _heatCapacityDirty = false;
            _heatCapacity = 0;

            IoCManager.Resolve(ref protoMan);
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            foreach (var (id, quantity) in Contents)
            {
                _heatCapacity += (float) quantity * prototypeManager.Index<ReagentPrototype>(id).SpecificHeat;
            }
            return _heatCapacity;
        }
    }
}
