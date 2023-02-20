using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.SCP.ConcreteSlab
{
    [NetworkedComponent]
    [Access(typeof(SharedSCP173System))]
    public abstract class SharedSCP173Component : Component
    {
        [DataField("enabled")]
        public bool Enabled = true;

        [DataField("lookedAt")]
        public bool LookedAt = false;
    }

    [Serializable, NetSerializable]
    public sealed class SCP173ComponentState : ComponentState
    {
        public bool Enabled { get; init; }
        public bool LookedAt { get; init; }

        public SCP173ComponentState(bool enabled, bool lookedAt)
        {
            Enabled = enabled;
            LookedAt = lookedAt;
        }
    }
}
