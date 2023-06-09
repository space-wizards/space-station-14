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
    public sealed class BurstHealComponent : Component
    {
        /// <summary>
        /// Fraction of health to heal to restore the mob to life. Use more than
        /// 0.5 to return from crit -> living in one shot.
        /// </summary>
        [DataField("pointsPerSec"), ViewVariables(VVAccess.ReadOnly)]
        public float HealFraction = 0.6f;

        /// <summary>
        /// Points of damage left at minimum on a revived subject
        /// </summary>
        [DataField("minDamageLeft"), ViewVariables(VVAccess.ReadOnly)]
        public float MinDamageLeft = 50.0f;

        /// <summary>
        /// Next time we are going to apply a heal
        /// </summary>
        [DataField("reviveTime", customTypeSerializer:typeof(TimeOffsetSerializer))]
        public TimeSpan HealTime;

        /// <summary>
        /// Each time we suffer damage, we reset the revive to a range between MinReviveTime-MaxReviveTime
        /// </summary>
        [DataField("minReviveTime", customTypeSerializer:typeof(TimeOffsetSerializer))]
        public float MinHealTime;

        /// <summary>
        /// Each time we suffer damage, we reset the revive to a range between MinReviveTime-MaxReviveTime
        /// </summary>
        [DataField("maxReviveTime", customTypeSerializer:typeof(TimeOffsetSerializer))]
        public float MaxHealTime;

    }
}
