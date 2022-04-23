using Content.Shared.Access.Systems;
using Content.Shared.PDA;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Access.Components
{
    // TODO BUI NETWORKING if ever clients can open their own BUI's (id card console, pda), then this data should be
    // networked.
    [RegisterComponent, NetworkedComponent]
    [Friend(typeof(SharedIdCardSystem), typeof(SharedPDASystem), typeof(SharedAgentIdCardSystem))]
    public sealed class IdCardComponent : Component
    {
        [DataField("originalOwnerName")]
        public string OriginalOwnerName = default!;

        [DataField("fullName")]
        public string? FullName;

        [DataField("jobTitle")]
        public string? JobTitle;
    }

    [Serializable, NetSerializable]
    public sealed class IdCardComponentState : ComponentState
    {
        public string? FullName;
        public string? JobTitle;

        public IdCardComponentState(string? fullName, string? jobTitle)
        {
            FullName = fullName;
            JobTitle = jobTitle;
        }
    }
}
