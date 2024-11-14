using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.Ghost.Roles.Components;

/// <summary>
/// This is used for a ghost role which can be toggled on and off at will, like a PAI.
/// </summary>
[RegisterComponent, Access(typeof(ToggleableGhostRoleSystem))]
public sealed partial class ToggleableGhostRoleComponent : Component
{
    [DataField]
    public string ExamineTextMindPresent = string.Empty;

    [DataField]
    public string ExamineTextMindSearching = string.Empty;

    [DataField]
    public string ExamineTextNoMind = string.Empty;

    [DataField]
    public string BeginSearchingText = string.Empty;

    [DataField]
    public string RoleName = string.Empty;

    [DataField]
    public string RoleDescription = string.Empty;

    [DataField]
    public string RoleRules = string.Empty;

    [DataField]
    public List<ProtoId<EntityPrototype>> MindRoles;

    [DataField]
    public string WipeVerbText = string.Empty;

    [DataField]
    public string WipeVerbPopup = string.Empty;

    [DataField]
    public string StopSearchVerbText = string.Empty;

    [DataField]
    public string StopSearchVerbPopup = string.Empty;

    [DataField("job")]
    public ProtoId<JobPrototype>? JobProto = null;
}
