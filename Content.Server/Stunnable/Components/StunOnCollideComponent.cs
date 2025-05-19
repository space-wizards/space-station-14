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

        /// <summary>
        /// Multiplier for a mob's walking speed
        /// </summary>
        [DataField]
        public float WalkSpeedModifier = 1f;

        /// <summary>
        /// Multiplier for a mob's sprinting speed
        /// </summary>
        [DataField]
        public float SprintSpeedModifier = 1f;

        /// <summary>
        /// Refresh Stun or Slowdown on hit
        /// </summary>
        [DataField]
        public bool Refresh = true;

        /// <summary>
        /// Should the entity try and stand automatically after being knocked down?
        /// </summary>
        [DataField]
        public bool AutoStand = true;

        /// <summary>
        /// Fixture we track for the collision.
        /// </summary>
        [DataField("fixture")] public string FixtureID = "projectile";
    }
}
