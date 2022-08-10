using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Suspicion
{
    [NetworkedComponent()]
    public abstract class SharedSuspicionRoleComponent : Component
    {
    }

    [Serializable, NetSerializable]
    public sealed class SuspicionRoleComponentState : ComponentState
    {
        public readonly string? Role;
        public readonly bool? Antagonist;
        public readonly (string name, EntityUid)[] Allies;

        public SuspicionRoleComponentState(string? role, bool? antagonist, (string name, EntityUid)[] allies)
        {
            Role = role;
            Antagonist = antagonist;
            Allies = allies;
        }
    }
}
