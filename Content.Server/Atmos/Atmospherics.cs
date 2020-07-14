namespace Content.Shared.Atmos
{
    /// <summary>
    ///     Class to store atmos constants.
    /// </summary>
    public static class Atmospherics
    {
        /// <summary>
        ///     The universal gas constant, in kPa*L/(K*mol)
        /// </summary>
        public const float R = 8.314462618f;

        /// <summary>
        ///     1 ATM in kPA.
        /// </summary>
        public const float OneAtmosphere = 101.325f;

        /// <summary>
        ///     -270.3ºC in K
        /// </summary>
        public const float TCMB = 2.7f;

        /// <summary>
        ///     0ºC in K
        /// </summary>
        public const float T0C = 273.15f;

        /// <summary>
        ///     20ºC in K
        /// </summary>
        public const float T20C = 293.15f;

        /// <summary>
        ///     Liters in a cell.
        /// </summary>
        public const float CellVolume = 2500f;

        /// <summary>
        ///     Moles in a 2.5 m^3 cell at 101.325 Pa and 20ºC
        /// </summary>
        public const float MolesCellStandard = (OneAtmosphere * CellVolume / (T20C * R));

        /// <summary>
        ///     Minimum number of moles a gas can have.
        /// </summary>
        public const float GasMinMoles = 0.00000005f;

        public const float OpenHeatTransferCoefficient = 0.4f;

        /// <summary>
        ///     Ratio of air that must move to/from a tile to reset group processing
        /// </summary>
        public const float MinimumAirRatioToSuspend = 0.1f;

        /// <summary>
        ///     Minimum ratio of air that must move to/from a tile
        /// </summary>
        public const float MinimumAirRatioToMove = 0.001f;

        /// <summary>
        ///     Minimum amount of air that has to move before a group processing can be suspended
        /// </summary>
        public const float MinimumAirToSuspend = (MolesCellStandard*MinimumAirRatioToSuspend);

        public const float MinimumTemperatureToMove = (T20C + 100f);

        public const float MinimumMolesDeltaToMove = (MolesCellStandard * MinimumAirRatioToMove);

        /// <summary>
        ///     Minimum temperature difference before group processing is suspended
        /// </summary>
        public const float MinimumTemperatureDeltaToSuspend = 4.0f;

        /// <summary>
        ///     Minimum temperature difference before the gas temperatures are just set to be equal.
        /// </summary>
        public const float MinimumTemperatureDeltaToConsider = 0.5f;

        /// <summary>
        ///     Minimum heat capacity.
        /// </summary>
        public const float MinimumHeatCapacity = 0.0003f;
    }
}
