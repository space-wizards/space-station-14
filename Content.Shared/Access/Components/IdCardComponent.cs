using Content.Shared.Access.Systems;
using Content.Shared.PDA;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Access.Components
{
    [RegisterComponent, NetworkedComponent]
    [AutoGenerateComponentState]
    [Access(typeof(SharedIdCardSystem), typeof(SharedPDASystem), typeof(SharedAgentIdCardSystem))]
    public sealed partial class IdCardComponent : Component
    {
        [DataField("fullName")]
        [AutoNetworkedField]
        [Access(typeof(SharedIdCardSystem), typeof(SharedPDASystem), typeof(SharedAgentIdCardSystem),
            Other = AccessPermissions.ReadWrite)] // FIXME Friends
        public string? FullName;

        [DataField("jobTitle")]
        [AutoNetworkedField]
        public string? JobTitle;
    }
}
