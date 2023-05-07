namespace Content.Server.Shuttles.Components
{
    [RegisterComponent]
    public sealed class ShuttleComponent : Component
    {
        [ViewVariables]
        public bool Enabled = true;

        [ViewVariables]
        public Vector2[] CenterOfThrust = new Vector2[4];

        /// <summary>
        /// Thrust gets multiplied by this value if it's for braking.
        /// </summary>
        public const float BrakeCoefficient = 1.5f;

        public const float MaxLinearVelocity = 10f;

        public const float MaxAngularVelocity = 1f;

        /// <summary>
        /// The cached thrust available for each cardinal direction
        /// </summary>
        [ViewVariables]
        public readonly float[] LinearThrust = new float[4];

        /// <summary>
        /// The thrusters contributing to each direction for impulse.
        /// </summary>
        public readonly List<EntityUid>[] LinearThrusters = new List<EntityUid>[4];

        /// <summary>
        /// The thrusters contributing to the angular impulse of the shuttle.
        /// </summary>
        public readonly List<EntityUid> AngularThrusters = new();

        [ViewVariables]
        public float AngularThrust = 0f;

        /// <summary>
        /// A bitmask of all the directions we are considered thrusting.
        /// </summary>
        [ViewVariables]
        public DirectionFlag ThrustDirections = DirectionFlag.None;
    }
}
