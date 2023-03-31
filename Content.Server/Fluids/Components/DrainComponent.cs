namespace Content.Server.Fluids.Components
{
    [RegisterComponent]
    public sealed class DrainComponent : Component
    {
        public const string SolutionName = "drainBuffer";

        [DataField("accumulator")]
        public float Accumulator = 0f;

        /// <summary>
        /// How many units per second the drain can absorb from the surrounding puddles.
        /// Divided by puddles, so if there are 5 puddles this will take 1/5 from each puddle.
        /// This will stay fixed to 1 second no matter what DrainFrequency is.
        /// </summary>
        [DataField("unitsPerSecond")]
        public float UnitsPerSecond = 6f;

        /// <summary>
        /// How many units are ejected from the buffer per second.
        /// </summary>
        [DataField("unitsDestroyedPerSecond")]
        public float UnitsDestroyedPerSecond = 1f;

        /// <summary>
        /// How many (unobstructed) tiles away the drain will
        /// drain puddles from.
        /// </summary>
        [DataField("range")]
        public float Range = 2f;

        /// <summary>
        /// How often in seconds the drain checks for puddles around it.
        /// If the EntityQuery seems a bit unperformant this can be increased.
        /// </summary>
        [DataField("drainFrequency")]
        public float DrainFrequency = 1f;
    }
}
