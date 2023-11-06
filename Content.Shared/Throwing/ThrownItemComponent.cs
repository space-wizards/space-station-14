using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Throwing
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class ThrownItemComponent : Component
    {
        /// <summary>
        ///     The entity that threw this entity.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
        public EntityUid? Thrower { get; set; }

        /// <summary>
        ///     The <see cref="IGameTiming.CurTime"/> timestamp at which this entity was thrown.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
        public TimeSpan? ThrownTime { get; set; }

        /// <summary>
        ///     Compared to <see cref="IGameTiming.CurTime"/> to land this entity, if any.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
        public TimeSpan? LandTime { get; set; }

        /// <summary>
        ///     Whether or not this entity was already landed.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
        public bool Landed { get; set; }

        /// <summary>
        ///     Whether or not to play a sound when the entity lands.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
        public bool PlayLandSound { get; set; }
    }

    [Serializable, NetSerializable]
    public sealed class ThrownItemComponentState : ComponentState
    {
        public NetEntity? Thrower { get; }

        public ThrownItemComponentState(NetEntity? thrower)
        {
            Thrower = thrower;
        }
    }
}
