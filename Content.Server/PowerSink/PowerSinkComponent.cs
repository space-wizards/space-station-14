using Content.Server.Explosion.EntitySystems;
using Content.Shared.Explosion;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.PowerSink
{
    /// <summary>
    /// Absorbs power up to its capacity when anchored then explodes.
    /// </summary>
    [RegisterComponent, AutoGenerateComponentPause]
    public sealed partial class PowerSinkComponent : Component
    {
        /// <summary>
        /// When the power sink is nearing its explosion, warn the crew so they can look for it
        /// (if they're not already).
        /// </summary>
        [DataField("sentImminentExplosionWarning")]
        public bool SentImminentExplosionWarningMessage;

        /// <summary>
        /// If explosion has been triggered, time at which to explode.
        /// </summary>
        [DataField]
        [AutoPausedField]
        public TimeSpan? ExplosionTime;

        /// <summary>
        /// The highest sound warning threshold that has been hit (plays sfx occasionally as explosion nears)
        /// </summary>
        [DataField]
        public float HighestWarningSoundThreshold;

        /// <summary>
        ///     The explosion prototype. This determines the damage types, the tile-break chance, and some visual
        ///     information (e.g., the light that the explosion gives off).
        /// </summary>
        [DataField]
        public ProtoId<ExplosionPrototype> ExplosionType = "MicroBomb";

        /// <summary>
        ///     The maximum intensity the explosion can have on a single tile. This limits the maximum damage and tile
        ///     break chance the explosion can achieve at any given location.
        /// </summary>
        [DataField]
        public float MaxTileIntensity = 40f;

        /// <summary>
        ///     How quickly the intensity drops off as you move away from the epicenter.
        /// </summary>
        [DataField]
        public float IntensitySlope = 8f;

        /// <summary>
        ///     The total intensity of this explosion. The radius of the explosion scales like the cube root of this
        ///     number (see <see cref="ExplosionSystem.RadiusToIntensity"/>).
        /// </summary>
        /// <remarks>
        ///     This number can be overridden by passing optional argument to <see
        ///     cref="ExplosionSystem.TriggerExplosive"/>.
        /// </remarks>
        [DataField]
        public float TotalIntensity = 4000f;

        /// <summary>
        ///     Factor used to scale the explosion intensity when calculating tile break chances. Allows for stronger
        ///     explosives that don't space tiles, without having to create a new explosion-type prototype.
        /// </summary>
        [DataField]
        public float TileBreakScale = 3f;

        /// <summary>
        ///     Maximum number of times that an explosive can break a tile. Currently, for normal space stations breaking a
        ///     tile twice will generally result in a vacuum.
        /// </summary>
        [DataField]
        public int MaxTileBreak = int.MaxValue;

        /// <summary>
        ///     Whether this explosive should be able to create a vacuum by breaking tiles.
        /// </summary>
        [DataField]
        public bool CanCreateVacuum = true;

        /// <summary>
        /// Sound that plays when the PowerSink has charged and is about to explode.
        /// </summary>
        [DataField]
        public SoundSpecifier ChargeFireSound = new SoundPathSpecifier("/Audio/Effects/PowerSink/charge_fire.ogg");

        /// <summary>
        /// Sound that plays at intervals when the PowerSink is charging.
        /// </summary>
        [DataField]
        public SoundSpecifier ElectricSound =
            new SoundPathSpecifier("/Audio/Effects/PowerSink/electric.ogg")
            {
                Params = AudioParams.Default
                    .WithVolume(15f) // audible even behind walls
                    .WithRolloffFactor(10)
            };
    }
}
