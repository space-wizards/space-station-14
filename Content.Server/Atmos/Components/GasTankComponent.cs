using Content.Shared.Atmos;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Atmos.Components
{
    [RegisterComponent]
    public sealed partial class GasTankComponent : Component, IGasMixtureHolder
    {
        public const float MaxExplosionRange = 26f;
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

        public EntityUid? ConnectStream;
        public EntityUid? DisconnectStream;

        [DataField("air"), ViewVariables(VVAccess.ReadWrite)]
        public GasMixture Air { get; set; } = new();

        /// <summary>
        ///     Pressure at which tank should be considered 'low' such as for internals.
        /// </summary>
        [DataField("tankLowPressure"), ViewVariables(VVAccess.ReadWrite)]
        public float TankLowPressure = DefaultLowPressure;

        /// <summary>
        ///     Distributed pressure.
        /// </summary>
        [DataField("outputPressure"), ViewVariables(VVAccess.ReadWrite)]
        public float OutputPressure = DefaultOutputPressure;

        /// <summary>
        ///     The maximum allowed output pressure.
        /// </summary>
        [DataField("maxOutputPressure"), ViewVariables(VVAccess.ReadWrite)]
        public float MaxOutputPressure = 3 * DefaultOutputPressure;

        /// <summary>
        ///     Tank is connected to internals.
        /// </summary>
        [ViewVariables]
        public bool IsConnected => User != null;

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
        [DataField("tankLeakPressure"), ViewVariables(VVAccess.ReadWrite)]
        public float TankLeakPressure = 30 * Atmospherics.OneAtmosphere;

        /// <summary>
        ///     Pressure at which tank spills all contents into atmosphere.
        /// </summary>
        [DataField("tankRupturePressure"), ViewVariables(VVAccess.ReadWrite)]
        public float TankRupturePressure = 40 * Atmospherics.OneAtmosphere;

        /// <summary>
        ///     Base 3x3 explosion.
        /// </summary>
        [DataField("tankFragmentPressure"), ViewVariables(VVAccess.ReadWrite)]
        public float TankFragmentPressure = 50 * Atmospherics.OneAtmosphere;

        /// <summary>
        ///     Increases explosion for each scale kPa above threshold.
        /// </summary>
        [DataField("tankFragmentScale"), ViewVariables(VVAccess.ReadWrite)]
        public float TankFragmentScale = 2 * Atmospherics.OneAtmosphere;

        [DataField("toggleAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string ToggleAction = "ActionToggleInternals";

        [DataField("toggleActionEntity")] public EntityUid? ToggleActionEntity;

        /// <summary>
        ///     Valve to release gas from tank
        /// </summary>
        [DataField("isValveOpen"), ViewVariables(VVAccess.ReadWrite)]
        public bool IsValveOpen = false;

        /// <summary>
        ///     Gas release rate in L/s
        /// </summary>
        [DataField("valveOutputRate"), ViewVariables(VVAccess.ReadWrite)]
        public float ValveOutputRate = 100f;

        [DataField("valveSound"), ViewVariables(VVAccess.ReadWrite)]
        public SoundSpecifier ValveSound =
            new SoundCollectionSpecifier("valveSqueak")
            {
                Params = AudioParams.Default.WithVolume(-5f),
            };
    }
}
