namespace Content.Server.AME.Components
{
    [RegisterComponent]
    public sealed class AMEFuelContainerComponent : Component
    {
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

        protected override void Initialize()
        {
            base.Initialize();
            _maxFuelAmount = 1000;
            _fuelAmount = 1000;
        }

    }
}
