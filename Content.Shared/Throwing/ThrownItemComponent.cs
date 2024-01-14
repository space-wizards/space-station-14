using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Throwing
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class ThrownItemComponent : Component
    {
        /// <summary>
        ///     The entity that threw this entity.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public EntityUid? Thrower;

        /// <summary>
        ///     The <see cref="IGameTiming.CurTime"/> timestamp at which this entity was thrown.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public TimeSpan? ThrownTime;

        /// <summary>
        ///     Compared to <see cref="IGameTiming.CurTime"/> to land this entity, if any.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public TimeSpan? LandTime;

        /// <summary>
        ///     Whether or not this entity was already landed.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public bool Landed;

        /// <summary>
        ///     Whether or not to play a sound when the entity lands.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public bool PlayLandSound;
    }

    [Serializable, NetSerializable]
    public sealed class ThrownItemComponentState : ComponentState
    {
        public NetEntity? Thrower;

        public TimeSpan? ThrownTime;

        public TimeSpan? LandTime;

        public bool Landed;

        public bool PlayLandSound;
    }
}
