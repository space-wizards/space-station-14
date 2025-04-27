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
    public LocId ExamineTextMindPresent { get; set; } = "positronic-brain-installed";

    /// <summary>
    /// The text shown on the entity's Examine when it is waiting for a controlling player
    /// </summary>
    [DataField]
    public LocId ExamineTextMindSearching { get; set; } = "positronic-brain-still-searching";

    /// <summary>
    /// The text shown on the entity's Examine when it has no controlling player
    /// </summary>
    [DataField]
    public LocId ExamineTextNoMind { get; set; } = "positronic-brain-off";

    /// <summary>
    /// The popup text when the entity (PAI/positronic brain) it is activated to seek a controlling player
    /// </summary>
    [DataField]
    public LocId BeginSearchingText { get; set; } = "positronic-brain-searching";

    /// <summary>
    /// The name shown on the Ghost Role list
    /// </summary>
    [DataField]
    public LocId RoleName { get; set; } = "positronic-brain-role-name";

    /// <summary>
    /// The description shown on the Ghost Role list
    /// </summary>
    [DataField]
    public LocId RoleDescription { get; set; } = "positronic-brain-role-description";

    /// <summary>
    /// The introductory message shown when trying to take the ghost role/join the raffle
    /// </summary>
    [DataField]
    public LocId RoleRules { get; set; } = "ghost-role-information-silicon-rules";

    /// <summary>
    /// A list of mind roles that will be added to the entity's mind
    /// </summary>
    [DataField]
    public List<EntProtoId> MindRoles;

    /// <summary>
    /// The displayed name of the verb to wipe the controlling player
    /// </summary>
    [DataField]
    public LocId WipeVerbText { get; set; } = "positronic-brain-wipe-device-verb-text";

    /// /// <summary>
    /// The popup message when wiping the controlling player
    /// </summary>
    [DataField]
    public LocId WipeVerbPopup { get; set; } = "positronic-brain-wiped-device";

    /// <summary>
    /// The displayed name of the verb to stop searching for a controlling player
    /// </summary>
    [DataField]
    public LocId StopSearchVerbText { get; set; } = "positronic-brain-stop-searching-verb-text";

    /// /// <summary>
    /// The popup message when stopping to search for a controlling player
    /// </summary>
    [DataField]
    public LocId StopSearchVerbPopup { get; set; } = "positronic-brain-stopped-searching";

    /// /// <summary>
    /// The prototype ID of the job that will be given to the controlling mind
    /// </summary>
    [DataField("job")]
    public ProtoId<JobPrototype>? JobProto;
}
