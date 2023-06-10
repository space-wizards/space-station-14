using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Zombies
{
    /// <summary>
    ///   For providing a flat heal each second to a living mob. Currently only used by zombies.
    /// </summary>
    [RegisterComponent, NetworkedComponent]
    public sealed class PassiveHealComponent : Component
    {
        /// <summary>
        /// How many points of damage are healed each second
        /// </summary>
        [DataField("pointsPerSec"), ViewVariables(VVAccess.ReadOnly)]
        public float PointsPerSec = 0.6f;

        /// <summary>
        /// Next time we are going to apply a heal
        /// </summary>
        [DataField("nextTick", customTypeSerializer:typeof(TimeOffsetSerializer))]
        public TimeSpan NextTick;
    }
}
