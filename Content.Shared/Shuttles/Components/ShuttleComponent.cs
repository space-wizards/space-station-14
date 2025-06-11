using Content.Shared.Shuttles.Systems;
using Robust.Shared.GameStates;
using System.Numerics;

namespace Content.Shared.Shuttles.Components
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class ShuttleComponent : Component
    {
        [ViewVariables, AutoNetworkedField]
        public bool Enabled = true;

        [ViewVariables, AutoNetworkedField]
        public Vector2[] CenterOfThrust = new Vector2[4];

        /// <summary>
        /// Thrust gets multiplied by this value if it's for braking.
        /// </summary>
        public const float BrakeCoefficient = 1.5f;

        /// <summary>
        /// Maximum velocity assuming unupgraded, tier 1 thrusters
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
        public float BaseMaxLinearVelocity = 60f;

        public const float MaxAngularVelocity = 4f;

        /// <summary>
        /// The cached thrust available for each cardinal direction
        /// </summary>
        [ViewVariables]
        public readonly float[] LinearThrust = new float[4];

        /// <summary>
        /// The thrusters contributing to each direction for impulse.
        /// </summary>
        // No touchy
        public readonly List<EntityUid>[] LinearThrusters = new List<EntityUid>[]
        {
            new(),
            new(),
            new(),
            new(),
        };

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

        /// <summary>
        /// Base damping modifier applied to the shuttle's physics component when not in FTL.
        /// </summary>
        [DataField]
        public float BodyModifier = 0.25f;

        /// <summary>
        /// Final Damping Modifier for a shuttle.
        /// This value is set to 0 during FTL. And to BodyModifier when not in FTL.
        /// </summary>
        [DataField]
        public float DampingModifier;

        //Starlight
        [DataField, AutoNetworkedField]
        public float FTLRange = 256f;

        [DataField, AutoNetworkedField]
        public bool FTLMassLimits = true;

        /// <summary>
        /// if set, the shuttle will try to dock to docking ports with this tag when it arrives at its destination if it can.
        /// </summary>
        [DataField, AutoNetworkedField]
        public string? PriorityTag = null;
        //end starlight
    }
}
