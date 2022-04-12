namespace Content.Server.Disease.Components
{
    [RegisterComponent]
    /// <summary>
    /// The component which gives an entity zombie traits.
    /// </summary>
    public sealed class DiseaseZombieComponent : Component
    {
        /// <summary>
        /// The probability that a given bite will infect a player.
        /// zombie infection is not based on disease resist items like masks or gloves.
        /// </summary>
        [DataField("probability")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float Probability = 0.33f;

        /// <summary>
        /// A multiplier on the movement speed that zombies recieve.
        /// </summary>
        [DataField("slowAmount")]
        public float SlowAmount = 0.75f;

        /// <summary>
        /// Whether or not the zombie needs all the zombie traits initialized upon component inti
        /// useful for entities that already are zombies and do not need the additional traits.
        /// </summary>
        [DataField("applyZombieTraits")]
        public bool ApplyZombieTraits = true;
    }
}
