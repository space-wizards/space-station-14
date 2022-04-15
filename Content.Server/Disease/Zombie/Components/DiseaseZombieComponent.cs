namespace Content.Server.Disease.Zombie.Components
{
    /// <summary>
    /// The component which gives an entity zombie traits.
    /// </summary>
    [RegisterComponent]
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
        [ViewVariables(VVAccess.ReadWrite)]
        public float SlowAmount = 0.75f;

        /// <summary>
        /// Whether or not the zombie needs all the zombie traits initialized upon component init
        /// useful for entities that already are zombies and do not need the additional traits.
        /// </summary>
        [DataField("applyZombieTraits")]
        public bool ApplyZombieTraits = true;

        /// <summary>
        /// The color of the zombie's skin
        /// </summary>
        [DataField("skinColor")]
        public readonly Color SkinColor = (0.70f, 0.72f, 0.48f, 1);
    }
}
