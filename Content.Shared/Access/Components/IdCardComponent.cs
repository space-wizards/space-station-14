using Content.Shared.Access.Systems;
using Content.Shared.PDA;

namespace Content.Shared.Access.Components
{
    // TODO BUI NETWORKING if ever clients can open their own BUI's (id card console, pda), then this data should be
    // networked.
    [RegisterComponent]
    [Access(typeof(SharedIdCardSystem), typeof(SharedPDASystem), typeof(SharedAgentIdCardSystem))]
    public sealed class IdCardComponent : Component
    {
        [DataField("originalOwnerName")]
        public string OriginalOwnerName = default!;

        [DataField("fullName")]
        [Access(typeof(SharedIdCardSystem), typeof(SharedPDASystem), typeof(SharedAgentIdCardSystem),
            Other = AccessPermissions.ReadWrite)] // FIXME Friends
        public string? FullName;

        [DataField("jobTitle")]
        public string? JobTitle;
    }
}
