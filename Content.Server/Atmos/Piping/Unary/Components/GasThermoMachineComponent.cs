using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping.Unary.Components;
using Content.Shared.Construction.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

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
        [ViewVariables(VVAccess.ReadWrite)]
        public float HeatCapacity = 10000;

        /// <summary>
        ///     Base heat capacity of the device. Actual heat capacity is calculated by taking this number and doubling
        ///     it for every matter bin quality tier above one.
        /// </summary>
        [DataField("baseHeatCapacity")]
        public float BaseHeatCapacity = 5000;

        [DataField("targetTemperature")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float TargetTemperature = Atmospherics.T20C;

        /// <summary>
        ///     Tolerance for temperature setpoint hysteresis.
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        public float TemperatureTolerance = 2f;

        /// <summary>
        ///     Implements setpoint hysteresis to prevent heater from rapidly cycling on and off at setpoint.
        ///     If true, add Sign(Cp)*TemperatureTolerance to the temperature setpoint.
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        public bool HysteresisState = false;

        /// <summary>
        ///     Coefficient of performance. Output power / input power.
        ///     Positive for heaters, negative for freezers.
        /// </summary>
        [DataField("coefficientOfPerformance")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float Cp = 0.9f; // output power / input power, positive is heat

        /// <summary>
        ///     Current minimum temperature, calculated from <see cref="InitialMinTemperature"/> and <see
        ///     cref="MinTemperatureDelta"/>.
        ///     Ignored if heater.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float MinTemperature;

        /// <summary>
        ///     Current maximum temperature, calculated from <see cref="InitialMaxTemperature"/> and <see
        ///     cref="MaxTemperatureDelta"/>.
        ///     Ignored if freezer.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float MaxTemperature;

        /// <summary>
        ///     Minimum temperature the device can reach with a 0 total capacitor quality. Usually the quality will be at
        ///     least 1.
        /// </summary>
        [DataField("baseMinTemperature")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float BaseMinTemperature = 96.625f; // Selected so that tier-1 parts can reach 73.15k

        /// <summary>
        ///     Maximum temperature the device can reach with a 0 total capacitor quality. Usually the quality will be at
        ///     least 1.
        /// </summary>
        [DataField("baseMaxTemperature")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float BaseMaxTemperature = Atmospherics.T20C;

        /// <summary>
        ///     Decrease in minimum temperature, per unit machine part quality.
        /// </summary>
        [DataField("minTemperatureDelta")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float MinTemperatureDelta = 23.475f; // selected so that tier-4 parts can reach TCMB

        /// <summary>
        ///     Change in maximum temperature, per unit machine part quality.
        /// </summary>
        [DataField("maxTemperatureDelta")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float MaxTemperatureDelta = 300;

        /// <summary>
        ///     The machine part that affects the heat capacity.
        /// </summary>
        [DataField("machinePartHeatCapacity", customTypeSerializer: typeof(PrototypeIdSerializer<MachinePartPrototype>))]
        public string MachinePartHeatCapacity = "MatterBin";

        /// <summary>
        ///     The machine part that affects the temperature range.
        /// </summary>
        [DataField("machinePartTemperature", customTypeSerializer: typeof(PrototypeIdSerializer<MachinePartPrototype>))]
        public string MachinePartTemperature = "Capacitor";

        /// <summary>
        /// Last amount of energy added/removed from the attached pipe network
        /// </summary>
        [DataField("lastEnergyDelta")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float LastEnergyDelta;
    }
}
