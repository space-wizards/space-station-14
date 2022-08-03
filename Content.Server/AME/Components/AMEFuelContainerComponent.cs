namespace Content.Server.AME.Components
{
    [RegisterComponent]
    public sealed class AMEFuelContainerComponent : Component
    {
        /// <summary>
        ///     The amount of fuel in the jar.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField("fuelAmount")]
        public int FuelAmount = 1000;

        /// <summary>
        ///     The maximum fuel capacity of the jar.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField("maxFuelAmount")]
        public int MaxFuelAmount = 1000;

        [ViewVariables(VVAccess.ReadWrite), DataField("sealed")]
        public bool Sealed = true;

        [ViewVariables(VVAccess.ReadOnly), DataField("openFuelConsumption")]
        public int OpenFuelConsumption = 32;

        [ViewVariables(VVAccess.ReadOnly), DataField("qualityNeeded")]
        public string QualityNeeded = "Prying";

        // Negative singulo food shrinks
        [ViewVariables(VVAccess.ReadOnly), DataField("singuloFoodPerThousand")]
        public int SinguloFoodPerThousand = -100;
    }
}
