using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping.Unary.Components;

namespace Content.Server.Atmos.Piping.Unary.Components
{
    [RegisterComponent]
    public sealed class GasVentPumpComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Enabled { get; set; } = true;

        [ViewVariables]
        public bool IsDirty { get; set; } = false;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Welded { get; set; } = false;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("inlet")]
        public string InletName { get; set; } = "pipe";

        [ViewVariables(VVAccess.ReadWrite)]
        public VentPumpDirection PumpDirection { get; set; } = VentPumpDirection.Releasing;

        [ViewVariables(VVAccess.ReadWrite)]
        public VentPressureBound PressureChecks { get; set; } = VentPressureBound.ExternalBound;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("externalPressureBound")]
        public float ExternalPressureBound { get; set; } = Atmospherics.OneAtmosphere;

        [ViewVariables(VVAccess.ReadWrite)]
        public float InternalPressureBound { get; set; } = 0f;

        /// <summary>
        ///     If the difference between the internal and external pressure is larger than this, the device can no
        ///     longer move gas.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("maxPressureDifference")]
        public float MaxPressureDifference = 4500;

        /// <summary>
        ///     Pressure pump speed. Determines how much gas is moved.
        /// </summary>
        /// <remarks>
        ///     The pump will attempt to modify the destination's final pressure by this quantity. The actual change
        ///     will be limited by efficiency losses as the pressure difference approaches <see
        ///     cref="MaxPressureDifference"/>.
        /// </remarks>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("pumpPressure")]
        public float PumpPressure = 100;
        // currently total-mole pumping capacity increases with available volume in the destination.

        public GasVentPumpData ToAirAlarmData()
        {
            if (!IsDirty) return new GasVentPumpData { Dirty = IsDirty };

            return new GasVentPumpData
            {
                Enabled = Enabled,
                Dirty = IsDirty,
                PumpDirection = PumpDirection,
                PressureChecks = PressureChecks,
                ExternalPressureBound = ExternalPressureBound,
                InternalPressureBound = InternalPressureBound
            };
        }

        public void FromAirAlarmData(GasVentPumpData data)
        {
            Enabled = data.Enabled;
            IsDirty = data.Dirty;
            PumpDirection = (VentPumpDirection) data.PumpDirection!;
            PressureChecks = (VentPressureBound) data.PressureChecks!;
            ExternalPressureBound = (float) data.ExternalPressureBound!;
            InternalPressureBound = (float) data.InternalPressureBound!;
        }
    }
}
