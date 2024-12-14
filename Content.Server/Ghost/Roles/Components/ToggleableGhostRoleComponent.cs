using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.Ghost.Roles.Components;

/// <summary>
/// This is used for a ghost role which can be toggled on and off at will, like a PAI.
/// </summary>
[RegisterComponent, Access(typeof(ToggleableGhostRoleSystem))]
public sealed partial class ToggleableGhostRoleComponent : Component
{
    [DataField("examineTextMindPresent")]
    public string ExamineTextMindPresent = string.Empty;

    [DataField("examineTextMindSearching")]
    public string ExamineTextMindSearching = string.Empty;

    [DataField("examineTextNoMind")]
    public string ExamineTextNoMind = string.Empty;

    [DataField("beginSearchingText")]
    public string BeginSearchingText = string.Empty;

    [DataField("roleName")]
    public string RoleName = string.Empty;

    [DataField("roleDescription")]
    public string RoleDescription = string.Empty;

    [DataField("roleRules")]
    public string RoleRules = string.Empty;

    [DataField("wipeVerbText")]
    public string WipeVerbText = string.Empty;

    [DataField("wipeVerbPopup")]
    public string WipeVerbPopup = string.Empty;

    [DataField("stopSearchVerbText")]
    public string StopSearchVerbText = string.Empty;

    [DataField("stopSearchVerbPopup")]
    public string StopSearchVerbPopup = string.Empty;

    [DataField("job")]
    public ProtoId<JobPrototype>? JobProto = null;
}
