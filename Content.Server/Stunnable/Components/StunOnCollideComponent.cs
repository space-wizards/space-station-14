namespace Content.Server.Stunnable.Components
{
    /// <summary>
    /// Adds stun when it collides with an entity
    /// </summary>
    [RegisterComponent, Access(typeof(StunOnCollideSystem))]
    public sealed partial class StunOnCollideComponent : Component
    {
        // TODO: Can probably predict this.

        /// <summary>
        /// how long a player will have stamina crit
        /// </summary>
        [DataField("stunAmount")]
        public double StunAmount;

        /// <summary>
        /// how long the player will be on the floor
        /// </summary>
        [DataField("knockdownAmount")]
        public double KnockdownAmount;

        /// <summary>
        /// how long the player will have a slowdown
        /// </summary>
        [DataField("slowdownAmount")]
        public double SlowdownAmount;

        // See stunsystem for what these do
        [DataField("walkSpeedMultiplier")]
        public float WalkSpeedMultiplier = 1f;

        [DataField("runSpeedMultiplier")]
        public float RunSpeedMultiplier = 1f;

        /// <summary>
        /// Fixture we track for the collision.
        /// </summary>
        [DataField("fixture")] public string FixtureID = "projectile";
    }
}
