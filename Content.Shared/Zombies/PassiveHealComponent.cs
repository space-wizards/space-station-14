using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Roles;
using Content.Shared.Humanoid;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using static Content.Shared.Humanoid.HumanoidAppearanceState;

namespace Content.Shared.Zombies
{
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
