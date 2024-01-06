using Content.Shared.Access.Systems;
using Content.Shared.PDA;
using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Access.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
[Access(typeof(SharedIdCardSystem), typeof(SharedPdaSystem), typeof(SharedAgentIdCardSystem), Other = AccessPermissions.ReadWrite)]
public sealed partial class IdCardComponent : Component
{
    [DataField("fullName"), ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    // FIXME Friends
    public string? FullName;

    [DataField("jobTitle")]
    [AutoNetworkedField]
    [Access(typeof(SharedIdCardSystem), typeof(SharedPdaSystem), typeof(SharedAgentIdCardSystem), Other = AccessPermissions.ReadWrite), ViewVariables(VVAccess.ReadWrite)]
    public string? JobTitle;

    /// <summary>
    /// The state of the job icon rsi.
    /// </summary>
    [DataField("jobIcon", customTypeSerializer: typeof(PrototypeIdSerializer<StatusIconPrototype>))]
    [AutoNetworkedField]
    public string JobIcon = "JobIconUnknown";

    /// <summary>
    /// The unlocalized names of the departments associated with the job
    /// </summary>
    [DataField("jobDepartments")]
    [AutoNetworkedField]
    public List<LocId> JobDepartments = new();

    /// <summary>
    /// Determines if accesses from this card should be logged by <see cref="AccessReaderComponent"/>
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool BypassLogging;
}
