using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Buckle.Components
{
    /// <summary>
    /// Component that makes an entity ignite entities that are buckled to it.
    /// </summary>
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentPause, AutoGenerateComponentState]
    public sealed partial class IgniteOnBuckleComponent : Component
    {
        /// <summary>
        /// How many fire stacks to add per cycle.
        /// </summary>
        [DataField, AutoNetworkedField]
        public float FireStacks = 1f;

        /// <summary>
        /// How frequently the ignition should be applied, in seconds.
        /// </summary>
        [DataField, AutoNetworkedField]
        public float IgniteTime = 1f;

        /// <summary>
        /// Maximum fire stacks that can be added by this source.
        /// If target already has this many or more fire stacks, no additional stacks will be added.
        /// 0 = unlimited.
        /// </summary>
        [DataField, AutoNetworkedField]
        public float? MaxFireStacks = null;

        /// <summary>
        /// Next time that fire stacks will be applied.
        /// </summary>
        [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField, AutoNetworkedField]
        public TimeSpan NextIgniteTime = TimeSpan.Zero;
    }
}
