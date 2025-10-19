using Robust.Shared.Serialization;
// ReSharper disable InconsistentNaming

namespace Content.Shared.Atmos
{
    /// <summary>
    ///     Class to store atmos constants.
    /// </summary>
    public static class Atmospherics
    {
        #region ATMOS
        /// <summary>
        ///     The universal gas constant, in kPa*L/(K*mol)
        /// </summary>
        public const float R = 8.314462618f;

        /// <summary>
        ///     1 ATM in kPA.
        /// </summary>
        public const float OneAtmosphere = 101.325f;

        /// <summary>
        ///     Maximum external pressure (in kPA) a gas miner will, by default, output to.
        ///     This is used to initialize roundstart atmos rooms.
        /// </summary>
        public const float GasMinerDefaultMaxExternalPressure = 6500f;

        /// <summary>
        ///     -270.3ºC in K. CMB stands for Cosmic Microwave Background.
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
        ///     -38.15ºC in K.
        ///     This is used to initialize roundstart freezer rooms.
        /// </summary>
        public const float FreezerTemp = 235f;

        /// <summary>
        ///     Do not allow any gas mixture temperatures to exceed this number. It is occasionally possible
        ///     to have very small heat capacity (e.g. room that was just unspaced) and for large amounts of
        ///     energy to be transferred to it, even for a brief moment. However, this messes up subsequent
        ///     calculations and so cap it here. The physical interpretation is that at this temperature, any
        ///     gas that you would have transforms into plasma.
        /// </summary>
        public const float Tmax = 262144; // 1/64 of max safe integer, any values above will result in a ~0.03K epsilon

        /// <summary>
        ///     Liters in a cell.
        /// </summary>
        public const float CellVolume = 2500f;

        // Liters in a normal breath
        public const float BreathVolume = 0.5f;

        // Amount of air to take from a tile
        public const float BreathPercentage = BreathVolume / CellVolume;

        /// <summary>
        ///     Moles in a 2.5 m^3 cell at 101.325 kPa and 20ºC
        /// </summary>
        public const float MolesCellStandard = (OneAtmosphere * CellVolume / (T20C * R));

        /// <summary>
        ///     Moles in a 2.5 m^3 cell at 101.325 kPa and -38.15ºC.
        ///     This is used in fix atmos freezer markers to ensure the air is at the correct atmospheric pressure while still being cold.
        /// </summary>
        public const float MolesCellFreezer = (OneAtmosphere * CellVolume / (FreezerTemp * R));

        /// <summary>
        ///     Moles in a 2.5 m^3 cell at GasMinerDefaultMaxExternalPressure kPa and 20ºC
        /// </summary>
        public const float MolesCellGasMiner = (GasMinerDefaultMaxExternalPressure * CellVolume / (T20C * R));

        /// <summary>
        ///     Compared against for superconduction.
        /// </summary>
        public const float MCellWithRatio = (MolesCellStandard * 0.005f);

        public const float OxygenStandard = 0.21f;
        public const float NitrogenStandard = 0.79f;

        public const float OxygenMolesStandard = MolesCellStandard * OxygenStandard;
        public const float NitrogenMolesStandard = MolesCellStandard * NitrogenStandard;

        public const float OxygenMolesFreezer = MolesCellFreezer * OxygenStandard;
        public const float NitrogenMolesFreezer = MolesCellFreezer * NitrogenStandard;

        public const float OxygenMolesGasMiner = MolesCellGasMiner * OxygenStandard;
        public const float NitrogenMolesGasMiner = MolesCellGasMiner * NitrogenStandard;

        #endregion

        /// <summary>
        ///     Visible moles multiplied by this factor to get moles at which gas is at max visibility.
        /// </summary>
        public const float FactorGasVisibleMax = 20f;

        /// <summary>
        ///     Minimum number of moles a gas can have.
        /// </summary>
        public const float GasMinMoles = 0.00000005f;

        public const float OpenHeatTransferCoefficient = 0.4f;

        /// <summary>
        ///     Hack to make vacuums cold, sacrificing realism for gameplay.
        /// </summary>
        public const float HeatCapacityVacuum = 7000f;

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
        public const float MinimumAirToSuspend = (MolesCellStandard * MinimumAirRatioToSuspend);

        public const float MinimumTemperatureToMove = (T20C + 100f);

        public const float MinimumMolesDeltaToMove = (MolesCellStandard * MinimumAirRatioToMove);

        /// <summary>
        ///     Minimum temperature difference before group processing is suspended
        /// </summary>
        public const float MinimumTemperatureDeltaToSuspend = 4.0f;

        /// <summary>
        ///     Minimum temperature difference before the gas temperatures are just set to be equal.
        /// </summary>
        public const float MinimumTemperatureDeltaToConsider = 0.01f;

        /// <summary>
        ///     Minimum temperature for starting superconduction.
        /// </summary>
        public const float MinimumTemperatureStartSuperConduction = (T20C + 400f);
        public const float MinimumTemperatureForSuperconduction = (T20C + 80f);

        /// <summary>
        ///     Minimum heat capacity.
        /// </summary>
        public const float MinimumHeatCapacity = 0.0003f;

        /// <summary>
        ///     For the purposes of making space "colder"
        /// </summary>
        public const float SpaceHeatCapacity = 7000f;

        /// <summary>
        ///     Dictionary of chemical abbreviations for <see cref="Gas"/>
        /// </summary>
        public static Dictionary<Gas, string> GasAbbreviations = new Dictionary<Gas, string>()
        {
            [Gas.Ammonia] = Loc.GetString("gas-ammonia-abbreviation"),
            [Gas.CarbonDioxide] = Loc.GetString("gas-carbon-dioxide-abbreviation"),
            [Gas.Frezon] = Loc.GetString("gas-frezon-abbreviation"),
            [Gas.Nitrogen] = Loc.GetString("gas-nitrogen-abbreviation"),
            [Gas.NitrousOxide] = Loc.GetString("gas-nitrous-oxide-abbreviation"),
            [Gas.Oxygen] = Loc.GetString("gas-oxygen-abbreviation"),
            [Gas.Plasma] = Loc.GetString("gas-plasma-abbreviation"),
            [Gas.Tritium] = Loc.GetString("gas-tritium-abbreviation"),
            [Gas.WaterVapor] = Loc.GetString("gas-water-vapor-abbreviation"),
        };

        #region Excited Groups

        /// <summary>
        ///     Number of full atmos updates ticks before an excited group breaks down (averages gas contents across turfs)
        /// </summary>
        public const int ExcitedGroupBreakdownCycles = 4;

        /// <summary>
        ///     Number of full atmos updates before an excited group dismantles and removes its turfs from active
        /// </summary>
        public const int ExcitedGroupsDismantleCycles = 16;

        #endregion

        /// <summary>
        ///     Hard limit for zone-based tile equalization.
        /// </summary>
        public const int MonstermosHardTileLimit = 2000;

        /// <summary>
        ///     Limit for zone-based tile equalization.
        /// </summary>
        public const int MonstermosTileLimit = 200;

        /// <summary>
        ///     Total number of gases. Increase this if you want to add more!
        /// </summary>
        public const int TotalNumberOfGases = 9;

        /// <summary>
        ///     This is the actual length of the gases arrays in mixtures.
        ///     Set to the closest multiple of 4 relative to <see cref="TotalNumberOfGases"/> for SIMD reasons.
        /// </summary>
        public const int AdjustedNumberOfGases = ((TotalNumberOfGases + 3) / 4) * 4;

        /// <summary>
        ///     Amount of heat released per mole of burnt hydrogen or tritium (hydrogen isotope)
        /// </summary>
        public const float FireHydrogenEnergyReleased = 284e3f; // hydrogen is 284 kJ/mol
        public const float FireMinimumTemperatureToExist = T0C + 100f;
        public const float FireMinimumTemperatureToSpread = T0C + 150f;
        public const float FireSpreadRadiosityScale = 0.85f;
        public const float FirePlasmaEnergyReleased = 160e3f; // methane is 16 kJ/mol, plus plasma's spark of magic
        public const float FireGrowthRate = 40000f;

        public const float SuperSaturationThreshold = 96f;
        public const float SuperSaturationEnds = SuperSaturationThreshold / 3;

        public const float OxygenBurnRateBase = 1.4f;
        public const float PlasmaMinimumBurnTemperature = (100f+T0C);
        public const float PlasmaUpperTemperature = (1370f+T0C);
        public const float PlasmaOxygenFullburn = 10f;
        public const float PlasmaBurnRateDelta = 9f;

        /// <summary>
        ///     This is calculated to help prevent singlecap bombs (Overpowered tritium/oxygen single tank bombs)
        /// </summary>
        public const float MinimumTritiumOxyburnEnergy = 143000f;

        public const float TritiumBurnOxyFactor = 100f;
        public const float TritiumBurnTritFactor = 10f;

        public const float FrezonCoolLowerTemperature = 23.15f;

        /// <summary>
        ///     Frezon cools better at higher temperatures.
        /// </summary>
        public const float FrezonCoolMidTemperature = 373.15f;

        public const float FrezonCoolMaximumEnergyModifier = 10f;

        /// <summary>
        ///     Remove X mol of nitrogen for each mol of frezon.
        /// </summary>
        public const float FrezonNitrogenCoolRatio = 5;
        public const float FrezonCoolEnergyReleased = -600e3f;
        public const float FrezonCoolRateModifier = 20f;

        public const float FrezonProductionMaxEfficiencyTemperature = 73.15f;

        /// <summary>
        ///     1 mol of N2 is required per X mol of tritium and oxygen.
        /// </summary>
        public const float FrezonProductionNitrogenRatio = 10f;

        /// <summary>
        ///     1 mol of Tritium is required per X mol of oxygen.
        /// </summary>
        public const float FrezonProductionTritRatio = 8.0f;

        /// <summary>
        ///     1 / X of the tritium is converted into Frezon each tick
        /// </summary>
        public const float FrezonProductionConversionRate = 50f;

        /// <summary>
        ///     The maximum portion of the N2O that can decompose each reaction tick. (50%)
        /// </summary>
        public const float N2ODecompositionRate = 2f;

        /// <summary>
        ///     Divisor for Ammonia Oxygen reaction so that it doesn't happen instantaneously.
        /// </summary>
        public const float AmmoniaOxygenReactionRate = 10f;

        /// <summary>
        ///     Determines at what pressure the ultra-high pressure red icon is displayed.
        /// </summary>
        public const float HazardHighPressure = 550f;

        /// <summary>
        ///     Determines when the orange pressure icon is displayed.
        /// </summary>
        public const float WarningHighPressure = 0.7f * HazardHighPressure;

        /// <summary>
        ///     Determines when the gray low pressure icon is displayed.
        /// </summary>
        public const float WarningLowPressure = 2.5f * HazardLowPressure;

        /// <summary>
        ///     Determines when the black ultra-low pressure icon is displayed.
        /// </summary>
        public const float HazardLowPressure = 20f;

        /// <summary>
        ///    The amount of pressure damage someone takes is equal to ((pressure / HAZARD_HIGH_PRESSURE) - 1)*PRESSURE_DAMAGE_COEFFICIENT,
        ///     with the maximum of MaxHighPressureDamage.
        /// </summary>
        public const float PressureDamageCoefficient = 4;

        /// <summary>
        ///     Maximum amount of damage that can be endured with high pressure.
        /// </summary>
        public const int MaxHighPressureDamage = 4;

        /// <summary>
        ///     The amount of damage someone takes when in a low pressure area
        ///     (The pressure threshold is so low that it doesn't make sense to do any calculations,
        ///     so it just applies this flat value).
        /// </summary>
        public const int LowPressureDamage = 4;

        public const float WindowHeatTransferCoefficient = 0.1f;

        /// <summary>
        ///     Directions that atmos currently supports. Modify in case of multi-z.
        ///     See <see cref="AtmosDirection"/> on the server.
        /// </summary>
        public const int Directions = 4;

        /// <summary>
        ///     The normal body temperature in degrees Celsius.
        /// </summary>
        public const float NormalBodyTemperature = 37f;

        /// <summary>
        ///     I hereby decree. This is Arbitrary Suck my Dick
        /// </summary>
        public const float BreathMolesToReagentMultiplier = 1144;

        #region Pipes

        /// <summary>
        ///     The default pressure at which pumps and powered equipment max out at, in kPa.
        /// </summary>
        public const float MaxOutputPressure = 4500;

        /// <summary>
        ///     The default maximum speed powered equipment can work at, in L/s.
        /// </summary>
        public const float MaxTransferRate = 200;

        #endregion
    }

    /// <summary>
    ///     Gases to Ids. Keep these updated with the prototypes!
    /// </summary>
    [Serializable, NetSerializable]
    public enum Gas : sbyte
    {
        Oxygen = 0,
        Nitrogen = 1,
        CarbonDioxide = 2,
        Plasma = 3,
        Tritium = 4,
        WaterVapor = 5,
        Ammonia = 6,
        NitrousOxide = 7,
        Frezon = 8
    }
}
