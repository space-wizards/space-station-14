using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Power.AME
{
    [RegisterComponent]
    public class AMEFuelContainerComponent : Component
    {
        public override string Name => "AMEFuelContainer";

        private int _fuelAmount;
        private int _maxFuelAmount;

        /// <summary>
        ///     The amount of fuel in the jar.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public int FuelAmount
        {
            get => _fuelAmount;
            set => _fuelAmount = value;
        }

        /// <summary>
        ///     The maximum fuel capacity of the jar.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public int MaxFuelAmount
        {
            get => _maxFuelAmount;
            set => _maxFuelAmount = value;
        }

        public override void Initialize()
        {
            base.Initialize();
            _maxFuelAmount = 1000;
            _fuelAmount = 1000;
        }

    }
}
