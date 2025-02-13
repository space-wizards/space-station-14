using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.Ghost.Roles.Components;

/// <summary>
/// This is used for a ghost role which can be toggled on and off at will, like a PAI.
/// </summary>
[RegisterComponent, Access(typeof(ToggleableGhostRoleSystem))]
public sealed partial class ToggleableGhostRoleComponent : Component
{
    /// <summary>
    /// The text shown on the entity's Examine when it is controlled by a player
    /// </summary>
    [DataField]
    public string ExamineTextMindPresent = string.Empty;

    /// <summary>
    /// The text shown on the entity's Examine when it is waiting for a controlling player
    /// </summary>
    [DataField]
    public string ExamineTextMindSearching = string.Empty;

    /// <summary>
    /// The text shown on the entity's Examine when it has no controlling player
    /// </summary>
    [DataField]
    public string ExamineTextNoMind = string.Empty;

    /// <summary>
    /// The popup text when the entity (PAI/positronic brain) it is activated to seek a controlling player
    /// </summary>
    [DataField]
    public string BeginSearchingText = string.Empty;

    /// <summary>
    /// The name shown on the Ghost Role list
    /// </summary>
    [DataField]
    public string RoleName = string.Empty;

    /// <summary>
    /// The description shown on the Ghost Role list
    /// </summary>
    [DataField]
    public string RoleDescription = string.Empty;

    /// <summary>
    /// The introductory message shown when trying to take the ghost role/join the raffle
    /// </summary>
    [DataField]
    public string RoleRules = string.Empty;

    /// <summary>
    /// A list of mind roles that will be added to the entity's mind
    /// </summary>
    [DataField]
    public List<EntProtoId> MindRoles;

    /// <summary>
    /// The displayed name of the verb to wipe the controlling player
    /// </summary>
    [DataField]
    public string WipeVerbText = string.Empty;

    /// /// <summary>
    /// The popup message when wiping the controlling player
    /// </summary>
    [DataField]
    public string WipeVerbPopup = string.Empty;

    /// <summary>
    /// The displayed name of the verb to stop searching for a controlling player
    /// </summary>
    [DataField]
    public string StopSearchVerbText = string.Empty;

    /// /// <summary>
    /// The popup message when stopping to search for a controlling player
    /// </summary>
    [DataField]
    public string StopSearchVerbPopup = string.Empty;

    /// /// <summary>
    /// The prototype ID of the job that will be given to the controlling mind
    /// </summary>
    [DataField("job")]
    public ProtoId<JobPrototype>? JobProto;
}
