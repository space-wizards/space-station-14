using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Stunnable.Components
{
    /// <summary>
    /// Adds stun when it collides with an entity
    /// </summary>
    [RegisterComponent, Friend(typeof(StunOnCollideSystem))]
    public sealed class StunOnCollideComponent : Component
    {
        // TODO: Can probably predict this.

        // See stunsystem for what these do
        [DataField("stunAmount")]
        public int StunAmount;

        [DataField("knockdownAmount")]
        public int KnockdownAmount;

        [DataField("slowdownAmount")]
        public int SlowdownAmount;

        [DataField("walkSpeedMultiplier")]
        public float WalkSpeedMultiplier = 1f;

        [DataField("runSpeedMultiplier")]
        public float RunSpeedMultiplier = 1f;
    }
}
