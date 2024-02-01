using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Throwing
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
    public sealed partial class ThrownItemComponent : Component
    {
        /// <summary>
        ///     The entity that threw this entity.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
        public EntityUid? Thrower;

        /// <summary>
        ///     The <see cref="IGameTiming.CurTime"/> timestamp at which this entity was thrown.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
        public TimeSpan? ThrownTime;

        /// <summary>
        ///     Compared to <see cref="IGameTiming.CurTime"/> to land this entity, if any.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
        public TimeSpan? LandTime;

        /// <summary>
        ///     Whether or not this entity was already landed.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
        public bool Landed;

        /// <summary>
        ///     Whether or not to play a sound when the entity lands.
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
        public bool PlayLandSound;

        /// <summary>
        ///     Used to restore state after the throwing scale animation is finished.
        /// </summary>
        [DataField]
        public Vector2? OriginalScale = null;
    }
}
