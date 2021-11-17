using System.Collections.Generic;
using Content.Shared.Shuttles.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Content.Server.Shuttles.Components
{
    [RegisterComponent]
    public sealed class ShuttleComponent : SharedShuttleComponent
    {
        /// <summary>
        /// The cached impulse available for each cardinal direction
        /// </summary>
        [ViewVariables]
        public readonly float[] LinearThrusterImpulse = new float[4];

        /// <summary>
        /// The thrusters contributing to each direction for impulse.
        /// </summary>
        public readonly List<ThrusterComponent>[] LinearThrusters = new List<ThrusterComponent>[4];

        /// <summary>
        /// The thrusters contributing to the angular impulse of the shuttle.
        /// </summary>
        public readonly List<ThrusterComponent> AngularThrusters = new List<ThrusterComponent>();

        [ViewVariables]
        public float AngularThrust = 0f;

        /// <summary>
        /// A bitmask of all the directions we are considered thrusting.
        /// </summary>
        [ViewVariables]
        public DirectionFlag ThrustDirections = DirectionFlag.None;
    }
}
