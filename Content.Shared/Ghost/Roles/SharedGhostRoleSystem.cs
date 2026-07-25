using Content.Shared.Ghost.Roles.Raffles;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Ghost.Roles;

[Serializable, NetSerializable]
public sealed class GhostRole
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public NetEntity Id;
}

/// <summary>
/// Struct that can be applied to an existing GhostRoleComponent by other systems to modify the ghost role.
/// See Content.Server.GhostRoleComponent for property definitions.
/// </summary>
[DataDefinition]
public sealed partial class GhostRoleSettings
{
    /// <summary>
    /// Whether the MakeSentientCommand should run on the mob.
    /// </summary>
    [DataField]
    public bool MakeSentient = true;

    /// <summary>
    /// The probability that this ghost role will be available after init.
    /// Used mostly for takeover roles that want some probability of being takeover, but not 100%.
    /// </summary>
    [DataField]
    public float Probability = 1f;

    /// <summary>
    /// Name of the role to display in the ghost role UI.
    /// </summary>
    [DataField]
    public string RoleName = "Unknown";

    /// <summary>
    /// Description of the role to display in the ghost role UI.
    /// </summary>
    [DataField]
    public string RoleDescription = "Unknown";

    /// <summary>
    /// Rules for the role to display in the ghost role UI when selected.
    /// </summary>
    [DataField]
    public string RoleRules = "ghost-role-component-default-rules";

    /// <summary>
    /// The mind roles that will be added to the mob's mind entity
    /// </summary>
    [DataField]
    public List<EntProtoId> MindRoles = new() { "MindRoleGhostRoleNeutral" };

    /// <summary>
    /// Can the ghost role speak?
    /// </summary>
    [DataField]
    public bool AllowSpeech = true;

    /// <summary>
    /// Can the ghost role move?
    /// </summary>
    [DataField]
    public bool AllowMovement = false;

    /// <summary>
    /// Reregisters the ghost role when the current player ghosts.
    /// </summary>
    [DataField]
    public bool ReregisterOnGhost = true;

    /// <summary>
    /// If set, ghost role is raffled, otherwise it is first-come-first-serve.
    /// </summary>
    [DataField]
    public GhostRoleRaffleConfig? RaffleConfig = null;

    /// <summary>
    /// Job the entity will receive after adding the mind.
    /// </summary>
    [DataField]
    public ProtoId<JobPrototype>? JobProto = null;
}
