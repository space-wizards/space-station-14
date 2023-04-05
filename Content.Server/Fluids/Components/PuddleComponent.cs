using Content.Server.Fluids.EntitySystems;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;

namespace Content.Server.Fluids.Components
{
    /// <summary>
    /// Puddle on a floor
    /// </summary>
    [RegisterComponent]
    [Access(typeof(PuddleSystem))]
    public sealed class PuddleComponent : Component
    {
        public const string DefaultSolutionName = "puddle";
        private static readonly FixedPoint2 DefaultSlipThreshold = FixedPoint2.New(-1); //Not slippery by default. Set specific slipThresholds in YAML if you want your puddles to be slippery. Lower = more slippery, and zero means any volume can slip.
        public static readonly FixedPoint2 DefaultOverflowVolume = FixedPoint2.New(20);

        // Current design: Something calls the SpillHelper.Spill, that will either
        // A) Add to an existing puddle at the location (normalised to tile-center) or
        // B) add a new one
        // From this every time a puddle is spilt on it will try and overflow to its neighbours if possible,
        // and also update its appearance based on volume level (opacity) and chemistry color
        // Small puddles will evaporate after a set delay

        // TODO: 'leaves fluidtracks', probably in a separate component for stuff like gibb chunks?;

        // based on behaviour (e.g. someone being punched vs slashed with a sword would have different blood sprite)
        // to check for low volumes for evaporation or whatever

        /// <summary>
        /// Puddles with volume above this threshold can slip players.
        /// </summary>
        [DataField("slipThreshold")]
        public FixedPoint2 SlipThreshold = DefaultSlipThreshold;

        [DataField("spillSound")]
        public SoundSpecifier SpillSound = new SoundPathSpecifier("/Audio/Effects/Fluids/splat.ogg");

        [DataField("overflowVolume")]
        public FixedPoint2 OverflowVolume = DefaultOverflowVolume;

        /// <summary>
        ///     How much should this puddle's opacity be multiplied by?
        ///     Useful for puddles that have a high overflow volume but still want to be mostly opaque.
        /// </summary>
        [DataField("opacityModifier")] public float OpacityModifier = 1.0f;

        [DataField("solution")] public string SolutionName { get; set; } = DefaultSolutionName;
    }
}
