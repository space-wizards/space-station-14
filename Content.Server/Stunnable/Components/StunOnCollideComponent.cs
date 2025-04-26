namespace Content.Server.Stunnable.Components
{
    /// <summary>
    /// Adds stun when it collides with an entity
    /// </summary>
    [RegisterComponent, Access(typeof(StunOnCollideSystem))]
    public sealed partial class StunOnCollideComponent : Component
    {
        // TODO: Can probably predict this.

        // See stunsystem for what these do
        [DataField]
        public TimeSpan StunAmount;

        [DataField]
        public TimeSpan KnockdownAmount;

        [DataField]
        public TimeSpan SlowdownAmount;

        [DataField]
        public float WalkSpeedMultiplier = 1f;

        [DataField]
        public float RunSpeedMultiplier = 1f;

        [DataField]
        public bool Refresh = true;

        [DataField]
        public bool AutoStand = true;

        /// <summary>
        /// Fixture we track for the collision.
        /// </summary>
        [DataField("fixture")] public string FixtureID = "projectile";
    }
}
