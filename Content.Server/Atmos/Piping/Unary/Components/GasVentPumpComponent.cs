using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping.Unary.Components;
using Content.Shared.MachineLinking;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

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
        public string Inlet { get; set; } = "pipe";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("outlet")]
        public string Outlet { get; set; } = "pipe";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("pumpDirection")]
        public VentPumpDirection PumpDirection { get; set; } = VentPumpDirection.Releasing;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("pressureChecks")]
        public VentPressureBound PressureChecks { get; set; } = VentPressureBound.ExternalBound;

        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("underPressureLockout")]
        public bool UnderPressureLockout { get; set; } = false;

        /// <summary>
        ///     In releasing mode, do not pump when environment pressure is below this limit.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("underPressureLockoutThreshold")]
        public float UnderPressureLockoutThreshold = 2;

        /// <summary>
        ///     Pressure locked vents still leak a little (leading to eventual pressurization of sealed sections)
        /// </summary>
        /// <remarks>
        ///     Ratio of pressure difference between pipes and atmosphere that will leak each second, in moles.
        ///     If the pipes are 200 kPa and the room is spaced, at 0.01 UnderPressureLockoutLeaking, the room will fill
        ///     at a rate of 2 moles / sec. It will then reach 2 kPa (UnderPressureLockoutThreshold) and begin normal
        ///     filling after about 20 seconds (depending on room size).
        ///
        ///     Since we want to prevent automating the work of atmos, the leaking rate of 0.0001f is set to make auto
        ///     repressurizing of the development map take about 30 minutes using an oxygen tank (high pressure)
        /// </remarks>

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("underPressureLockoutLeaking")]
        public float UnderPressureLockoutLeaking = 0.0001f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("externalPressureBound")]
        public float ExternalPressureBound
        {
            get => _externalPressureBound;
            set
            {
                _externalPressureBound = Math.Clamp(value, 0, MaxPressure);
            }
        }

        private float _externalPressureBound = Atmospherics.OneAtmosphere;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("internalPressureBound")]
        public float InternalPressureBound
        {
            get => _internalPressureBound;
            set
            {
                _internalPressureBound = Math.Clamp(value, 0, MaxPressure);
            }
        }

        private float _internalPressureBound = 0;

        /// <summary>
        ///     Max pressure of the target gas (NOT relative to source).
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("maxPressure")]
        public float MaxPressure = Atmospherics.MaxOutputPressure;

        /// <summary>
        ///     Pressure pump speed in kPa/s. Determines how much gas is moved.
        /// </summary>
        /// <remarks>
        ///     The pump will attempt to modify the destination's final pressure by this quantity every second. If this
        ///     is too high, and the vent is connected to a large pipe-net, then someone can nearly instantly flood a
        ///     room with gas.
        /// </remarks>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("targetPressureChange")]
        public float TargetPressureChange = Atmospherics.OneAtmosphere;

        /// <summary>
        ///     Ratio of max output air pressure and pipe pressure, representing the vent's ability to increase pressure
        /// </summary>
        /// <remarks>
        ///     Vents cannot suck a pipe completely empty, instead pressurizing a section to a max of
        ///     pipe pressure * PumpPower (in kPa). So a 51 kPa pipe is required for 101 kPA sections at PumpPower 2.0
        /// </remarks>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("PumpPower")]
        public float PumpPower = 2.0f;

        #region Machine Linking
        /// <summary>
        ///     Whether or not machine linking is enabled for this component.
        /// </summary>
        [DataField("canLink")]
        public readonly bool CanLink = false;

        [DataField("pressurizePort", customTypeSerializer: typeof(PrototypeIdSerializer<ReceiverPortPrototype>))]
        public string PressurizePort = "Pressurize";

        [DataField("depressurizePort", customTypeSerializer: typeof(PrototypeIdSerializer<ReceiverPortPrototype>))]
        public string DepressurizePort = "Depressurize";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("pressurizePressure")]
        public float PressurizePressure = Atmospherics.OneAtmosphere;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("depressurizePressure")]
        public float DepressurizePressure = 0;
        #endregion

        public GasVentPumpData ToAirAlarmData()
        {
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
            PumpDirection = data.PumpDirection;
            PressureChecks = data.PressureChecks;
            ExternalPressureBound = data.ExternalPressureBound;
            InternalPressureBound = data.InternalPressureBound;
        }
    }
}
