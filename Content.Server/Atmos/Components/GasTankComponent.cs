using Content.Shared.Actions.ActionTypes;
using Content.Shared.Atmos;
using Robust.Shared.Audio;

namespace Content.Server.Atmos.Components
{
    [RegisterComponent]
    public sealed class GasTankComponent : Component, IGasMixtureHolder
    {
        public const float MaxExplosionRange = 80f;
        private const float DefaultLowPressure = 0f;
        private const float DefaultOutputPressure = Atmospherics.OneAtmosphere;

        public int Integrity = 3;
        public bool IsLowPressure => (Air?.Pressure ?? 0F) <= TankLowPressure;

        [ViewVariables(VVAccess.ReadWrite), DataField("ruptureSound")]
        public SoundSpecifier RuptureSound = new SoundPathSpecifier("/Audio/Effects/spray.ogg");

        [ViewVariables(VVAccess.ReadWrite), DataField("connectSound")]
        public SoundSpecifier? ConnectSound =
            new SoundPathSpecifier("/Audio/Effects/internals.ogg")
            {
                Params = AudioParams.Default.WithVolume(5f),
            };

        [ViewVariables(VVAccess.ReadWrite), DataField("disconnectSound")]
        public SoundSpecifier? DisconnectSound;

        // Cancel toggles sounds if we re-toggle again.

        public IPlayingAudioStream? ConnectStream;
        public IPlayingAudioStream? DisconnectStream;

        [DataField("air")] public GasMixture Air { get; set; } = new();

        /// <summary>
        ///     Pressure at which tank should be considered 'low' such as for internals.
        /// </summary>
        [DataField("tankLowPressure")]
        public float TankLowPressure { get; set; } = DefaultLowPressure;

        /// <summary>
        ///     Distributed pressure.
        /// </summary>
        [DataField("outputPressure")]
        public float OutputPressure { get; set; } = DefaultOutputPressure;

        /// <summary>
        ///     Tank is connected to internals.
        /// </summary>
        [ViewVariables] public bool IsConnected => User != null;

        [ViewVariables]
        public EntityUid? User;

        /// <summary>
        ///     True if this entity was recently moved out of a container. This might have been a hand -> inventory
        ///     transfer, or it might have been the user dropping the tank. This indicates the tank needs to be checked.
        /// </summary>
        [ViewVariables]
        public bool CheckUser;

        /// <summary>
        ///     Pressure at which tanks start leaking.
        /// </summary>
        [DataField("tankLeakPressure")]
        public float TankLeakPressure { get; set; }     = 30 * Atmospherics.OneAtmosphere;

        /// <summary>
        ///     Pressure at which tank spills all contents into atmosphere.
        /// </summary>
        [DataField("tankRupturePressure")]
        public float TankRupturePressure { get; set; }  = 40 * Atmospherics.OneAtmosphere;

        /// <summary>
        ///     Base 3x3 explosion.
        /// </summary>
        [DataField("tankFragmentPressure")]
        public float TankFragmentPressure { get; set; } = 50 * Atmospherics.OneAtmosphere;

        /// <summary>
        ///     Increases explosion for each scale kPa above threshold.
        /// </summary>
        [DataField("tankFragmentScale")]
        public float TankFragmentScale { get; set; }    = 2 * Atmospherics.OneAtmosphere;

        [DataField("toggleAction", required: true)]
        public InstantAction ToggleAction = new();
    }
}
