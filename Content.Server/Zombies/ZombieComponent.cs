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
    }
}
