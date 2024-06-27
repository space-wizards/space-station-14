using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping.Unary.Components;
using Content.Shared.DeviceLinking;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Atmos.Piping.Unary.Components
{
    // The world if people documented their shit.
    [RegisterComponent]
    public sealed partial class GasVentPumpComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Enabled { get; set; } = true;

        [ViewVariables]
        public bool IsDirty { get; set; } = false;

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
        public float UnderPressureLockoutThreshold = 60; // this must be tuned in conjunction with atmos.mmos_spacing_speed

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

        #region fields used by GasVentPumpSystem.pressurizationLockout
        [ViewVariables(VVAccess.ReadOnly)]
        public int Samples { get; set; } = 0;

        /// <summary>
        ///     Calculate average pressure over X atmos updates.
        ///     Can't exceed compile-time setting <see cref=AveragingBufferSize>
        /// </summary>
        /// <remarks>
        ///     Too low values increase the likelyhood of the vent starting to cycle between pressurizing and locking out in a very slow spacing
        ///     scenario.
        /// </remarks>
        [ViewVariables(VVAccess.ReadWrite)]
        public int MaxSamples
        {
            get => _maxSamples;
            set => _maxSamples = Math.Clamp(value, 1, AveragingBufferSize);
        }
        private int _maxSamples = AveragingBufferSize;

        /// <Summary>
        ///     Maximum size for ring buffers used by GasVentPumpSystem.pressurizationLockout.
        ///     <see cref=MaxSamples> can't be set to higher than this value.
        /// </Summary>
        static readonly public int AveragingBufferSize = 5;

        [ViewVariables(VVAccess.ReadOnly)]
        public float[] Measurements { get; set; } = new float[AveragingBufferSize];

        [ViewVariables(VVAccess.ReadOnly)]
        public float[] PressurizationRate { get; set; } = new float[AveragingBufferSize];

        /// <summary>
        ///     The vent will be shut down if the average pressure drop over the samplesize is below X kPa/s.
        /// </summary>
        /// <remarks>
        ///     The samplesize is set via <see cref=GasVentPumpComponent.MaxSamples>
        ///
        ///     With X<0, drops in pressure cause lockout,
        ///     with X>0, vents only turn on when pressure is already rising.
        ///
        ///     In my testing, in a underPressureLockout failure the average pressure
        ///     drop over 5 ticks may drop to -0.04, triggering this check and causing a lockout, solving the
        ///     underPressureLockout failure.
        ///
        ///     This could be set to 0, but then the vents will stay locked for annoyingly long time whenever air flows
        ///     from this vent to the surroundings. This may cause pressure drops in the order of E-5 kPa/s, causing lockouts
        ///     while refilling rooms.
        ///     I'm not entirely sure if I got the math right, so the value -1 may not be exactly -1 kPa/s
        /// </remarks>
        [ViewVariables(VVAccess.ReadWrite)]
        public float PressurizationLockout { get; set; } = -0.02f;

        /// <summary>
        ///     Used to hold pressure from last tick. doesn't really matter what
        ///     it's initialized to.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float AveragePressure { get; set; } = 101.325f;

        [ViewVariables(VVAccess.ReadOnly)]
        public int WindowIdx { get; set; } = 0;
        #endregion

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
        public bool CanLink = false;

        [DataField("pressurizePort", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
        public string PressurizePort = "Pressurize";

        [DataField("depressurizePort", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
        public string DepressurizePort = "Depressurize";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("pressurizePressure")]
        public float PressurizePressure = Atmospherics.OneAtmosphere;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("depressurizePressure")]
        public float DepressurizePressure = 0;

        // When true, ignore under-pressure lockout. Used to re-fill rooms in air alarm "Fill" mode.
        [DataField]
        public bool PressureLockoutOverride = false;
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
                InternalPressureBound = InternalPressureBound,
                PressureLockoutOverride = PressureLockoutOverride
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
            PressureLockoutOverride = data.PressureLockoutOverride;
        }
    }
}
