using Content.Shared.FixedPoint;
using Robust.Client.Graphics;

namespace Content.Client.Fluids
{
    [RegisterComponent]
    public sealed class PuddleVisualizerComponent : Component
    {
        // Whether the underlying solution color should be used. True in most cases.
        [DataField("recolor")] public bool Recolor = true;

        // Whether the puddle has a unique sprite we don't want to overwrite
        [DataField("customPuddleSprite")] public bool CustomPuddleSprite;

        // Puddles may change which RSI they use for their sprites (e.g. wet floor effects). This field will store the original RSI they used.
        [DataField("originalRsi")] public RSI? OriginalRsi;

        /// <summary>
        /// Puddles with volume below this threshold are able to have their sprite changed to a wet floor effect, though this is not the only factor.
        /// </summary>
        [DataField("wetFloorEffectThreshold")]
        public FixedPoint2 WetFloorEffectThreshold = FixedPoint2.New(5);

        /// <summary>
        /// Alpha (opacity) of the wet floor sparkle effect. Higher alpha = more opaque/visible.
        /// </summary>
        [DataField("wetFloorEffectAlpha")]
        public float WetFloorEffectAlpha = 0.75f; //should be somewhat transparent by default.
    }
}
