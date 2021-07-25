using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Stunnable.Components
{
    /// <summary>
    /// Adds stun when it collides with an entity
    /// </summary>
    [RegisterComponent]
    internal sealed class StunOnCollideComponent : Component
    {
        // TODO: Can probably predict this.
        public override string Name => "StunOnCollide";

        // See stunnable for what these do
        [DataField("stunAmount")]
        internal int StunAmount = default;
        [DataField("knockdownAmount")]
        internal int KnockdownAmount = default;
        [DataField("slowdownAmount")]
        internal int SlowdownAmount = default;
    }
}
