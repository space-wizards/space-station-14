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
        public float UnderPressureLockoutThreshold = 1;

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
