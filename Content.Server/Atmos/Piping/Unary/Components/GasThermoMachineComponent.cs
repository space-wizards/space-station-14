using Content.Shared.Atmos;

namespace Content.Server.Atmos.Piping.Unary.Components
{
    [RegisterComponent]
    public sealed partial class GasThermoMachineComponent : Component
    {
        [DataField("inlet")]
        public string InletName = "pipe";

        /// <summary>
        ///     Current electrical power consumption, in watts. Increasing power increases the ability of the
        ///     thermomachine to heat or cool air.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public float HeatCapacity = 5000;

        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public float TargetTemperature = Atmospherics.T20C;

        /// <summary>
        ///     Tolerance for temperature setpoint hysteresis.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadOnly)]
        public float TemperatureTolerance = 2f;

        /// <summary>
        ///     Implements setpoint hysteresis to prevent heater from rapidly cycling on and off at setpoint.
        ///     If true, add Sign(Cp)*TemperatureTolerance to the temperature setpoint.
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        public bool HysteresisState;

        /// <summary>
        ///     Coefficient of performance. Output power / input power.
        ///     Positive for heaters, negative for freezers.
        /// </summary>
        [DataField("coefficientOfPerformance")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float Cp = 0.9f; // output power / input power, positive is heat

        /// <summary>
        ///     Current minimum temperature
        ///     Ignored if heater.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public float MinTemperature = 73.15f;

        /// <summary>
        ///     Current maximum temperature
        ///     Ignored if freezer.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public float MaxTemperature = 593.15f;

        /// <summary>
        /// Last amount of energy added/removed from the attached pipe network
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public float LastEnergyDelta;

        /// <summary>
        /// An percentage of the energy change that is leaked into the surrounding environment rather than the inlet pipe.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public float EnergyLeakPercentage;
    }
}
