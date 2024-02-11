using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.PowerSink
{
    /// <summary>
    /// Absorbs power up to its capacity when anchored then explodes.
    /// </summary>
    [RegisterComponent]
    public sealed partial class PowerSinkComponent : Component
    {
        /// <summary>
        /// When the power sink is nearing its explosion, warn the crew so they can look for it
        /// (if they're not already).
        /// </summary>
        [DataField("sentImminentExplosionWarning")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool SentImminentExplosionWarningMessage = false;

        /// <summary>
        /// If explosion has been triggered, time at which to explode.
        /// </summary>
        [DataField("explosionTime", customTypeSerializer:typeof(TimeOffsetSerializer))]
        public System.TimeSpan? ExplosionTime = null;

        /// <summary>
        /// The highest sound warning threshold that has been hit (plays sfx occasionally as explosion nears)
        /// </summary>
        [DataField("highestWarningSoundThreshold")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float HighestWarningSoundThreshold = 0f;

        [DataField("chargeFireSound")]
        public SoundSpecifier ChargeFireSound = new SoundPathSpecifier("/Audio/Effects/PowerSink/charge_fire.ogg");

        [DataField("electricSound")] public SoundSpecifier ElectricSound =
            new SoundPathSpecifier("/Audio/Effects/PowerSink/electric.ogg")
            {
                Params = AudioParams.Default
                    .WithVolume(15f) // audible even behind walls
                    .WithRolloffFactor(10)
            };
    }
}
