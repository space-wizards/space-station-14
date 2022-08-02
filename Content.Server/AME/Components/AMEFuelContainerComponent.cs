namespace Content.Server.AME.Components
{
    [RegisterComponent]
    public sealed class AMEFuelContainerComponent : Component
    {
        /// <summary>
        ///     The amount of fuel in the jar.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public int FuelAmount { get; set; }

        /// <summary>
        ///     The maximum fuel capacity of the jar.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public int MaxFuelAmount { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Sealed { get; set; }

        [ViewVariables(VVAccess.ReadOnly)] public int OpenFuelConsumption = 32;
        [ViewVariables(VVAccess.ReadOnly)] public string QualityNeeded = "Prying";

        protected override void Initialize()
        {
            base.Initialize();
            MaxFuelAmount = 1000;
            FuelAmount = 1000;
            Sealed = true;
        }

    }
}
