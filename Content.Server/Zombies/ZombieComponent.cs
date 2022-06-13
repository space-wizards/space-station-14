namespace Content.Server.Zombies
{
    [RegisterComponent]
    public sealed class ZombieComponent : Component
    {
        /// <summary>
        /// The coefficient of the damage reduction applied when a zombie
        /// attacks another zombie. longe name
        /// </summary>
        public float OtherZombieDamageCoefficient = 0.75f;

        /// <summary>
        /// The baseline infection chance you have if you are completely nude
        /// </summary>
        public float MaxZombieInfectionChance = 0.75f;

        /// <summary>
        /// The minimum infection chance possible. This is simply to prevent
        /// being invincible by bundling up.
        /// </summary>
        public float MinZombieInfectionChance = 0.1f;
    }
}
