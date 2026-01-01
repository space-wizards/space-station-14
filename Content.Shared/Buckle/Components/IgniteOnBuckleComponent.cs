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
        [DataField]
        public float FireStacks = 1f;

        /// <summary>
        /// How frequently the ignition should be applied, in seconds.
        /// </summary>
        [DataField]
        public float IgniteTime = 1f;

        /// <summary>
        /// Next time that fire stacks will be applied.
        /// </summary>
        [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField, AutoNetworkedField]
        public TimeSpan NextIgniteTime = TimeSpan.Zero;
    }
}
