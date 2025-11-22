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
    [DataField]
    public bool MakeSentient = true;

    [DataField]
    public float Probability = 1f;

    [DataField]
    public string RoleName = "Unknown";

    [DataField]
    public string RoleDescription = "Unknown";

    [DataField]
    public string RoleRules = "ghost-role-component-default-rules";

    [DataField]
    public List<EntProtoId> MindRoles = new() { "MindRoleGhostRoleNeutral" };

    [DataField]
    public bool AllowSpeech = true;

    [DataField]
    public bool AllowMovement = false;

    [DataField]
    public bool ReregisterOnGhost = true;

    [DataField]
    public GhostRoleRaffleConfig? RaffleConfig = null;

    [DataField]
    public ProtoId<JobPrototype>? JobProto = null;
}
