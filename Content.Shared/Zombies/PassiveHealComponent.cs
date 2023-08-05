using Content.Shared.Damage;
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
        /// How many flat points of damage are healed each second
        /// </summary>
        [DataField("FlatPerSec"), ViewVariables(VVAccess.ReadOnly)]
        public float FlatPerSec = 0.6f;

        /// <summary>
        /// Specific healing points per second. Specify negative values to heal.
        /// </summary>
        [DataField("healPerSec")]
        public DamageSpecifier? healPerSec;

        /// <summary>
        /// Next time we are going to apply a heal
        /// </summary>
        [DataField("nextTick", customTypeSerializer:typeof(TimeOffsetSerializer))]
        public TimeSpan NextTick;
    }
}
