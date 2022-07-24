using Content.Shared.Shuttles.Components;

namespace Content.Server.Shuttles.Components
{
    [RegisterComponent]
    public sealed class ShuttleComponent : Component
    {
        [ViewVariables]
        public bool Enabled = true;

        /// <summary>
        /// The cached thrust available for each cardinal direction
        /// </summary>
        [ViewVariables]
        public readonly float[] LinearThrust = new float[4];

        /// <summary>
        /// The thrusters contributing to each direction for impulse.
        /// </summary>
        public readonly List<ThrusterComponent>[] LinearThrusters = new List<ThrusterComponent>[4];

        /// <summary>
        /// The thrusters contributing to the angular impulse of the shuttle.
        /// </summary>
        public readonly List<ThrusterComponent> AngularThrusters = new();

        [ViewVariables]
        public float AngularThrust = 0f;

        /// <summary>
        /// A bitmask of all the directions we are considered thrusting.
        /// </summary>
        [ViewVariables]
        public DirectionFlag ThrustDirections = DirectionFlag.None;
    }
}
