using Content.Shared.FixedPoint;

namespace Content.Client.Fluids
{
    [RegisterComponent]
    public sealed class PuddleVisualizerComponent : Component
    {
        // Whether the underlying solution color should be used. True in most cases.
        [DataField("recolor")] public bool Recolor = true;

        // Whether the puddle has a unique sprite we don't want to overwrite
        [DataField("customPuddleSprite")] public bool CustomPuddleSprite;

        /// <summary>
        /// Puddles with volume below this threshold will have their sprite changed to a wet floor effect,
        /// provided they are in the process of evaporating.
        /// </summary>
        [DataField("wetFloorEffectThreshold")]
        public FixedPoint2 WetFloorEffectThreshold = FixedPoint2.New(5);
    }
}
