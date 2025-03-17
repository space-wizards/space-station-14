using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Contraband;

/// <summary>
/// This is used for marking entities that are considered 'contraband' IC and showing it clearly in examine.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ContrabandSystem)), AutoGenerateComponentState]
public sealed partial class ContrabandComponent : Component
{
    /// <summary>
    ///     The degree of contraband severity this item is considered to have.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public ProtoId<ContrabandSeverityPrototype> Severity = "Restricted";

    /// <summary>
    ///     Which departments is this item restricted to?
    ///     By default, command and sec are assumed to be fine with contraband.
    ///     If null, no departments are allowed to use this.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public HashSet<ProtoId<DepartmentPrototype>> AllowedDepartments = new();

    /// <summary>
    ///     Which jobs is this item restricted to?
    ///     If empty, no jobs are allowed to use this beyond the allowed departments.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public HashSet<ProtoId<JobPrototype>> AllowedJobs = new();

    /// <summary>
    ///     Imp addition. Departments allowed through this will not appear in the examine text.
    ///     Useful for allowing departments like CentComm to be allowed without it needing to be explicilty stated.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public HashSet<ProtoId<DepartmentPrototype>> ImplicitlyAllowedDepartments = new();

    /// <summary>
    ///     Imp addition. Jobs allowed through this will not appear in the examine text.
    ///     Useful for allowing jobs like captain or other heads of staff to be allowed without it needing to be explicilty stated.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public HashSet<ProtoId<JobPrototype>> ImplicitlyAllowedJobs = new();
}
