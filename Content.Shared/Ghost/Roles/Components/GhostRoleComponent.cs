using Content.Shared.Ghost.Roles.Raffles;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared.Ghost.Roles.Components;

[RegisterComponent]
[Access(typeof(SharedGhostRoleSystem))]
public sealed partial class GhostRoleComponent : Component
{
    [DataField("name")]
    [Access(typeof(SharedGhostRoleSystem), Other = AccessPermissions.ReadWriteExecute)]
    public LocId RoleName = "generic-unknown-title";

    [DataField("description")]
    [Access(typeof(SharedGhostRoleSystem), Other = AccessPermissions.ReadWriteExecute)]
    public LocId RoleDescription = "generic-unknown-title";

    [DataField("rules")]
    [Access(typeof(SharedGhostRoleSystem), Other = AccessPermissions.ReadWriteExecute)]
    public LocId RoleRules = "ghost-role-component-default-rules";

    /// <summary>
    /// Whether the <see cref="MakeSentientCommand"/> should run on the mob.
    /// </summary>
    [DataField]
    public bool MakeSentient = true;

    /// <summary>
    /// The probability that this ghost role will be available after init.
    /// Used mostly for takeover roles that want some probability of being takeover, but not 100%.
    /// </summary>
    [DataField("prob")]
    public float Probability = 1f;

    /// <summary>
    /// The mind roles that will be added to the mob's mind entity
    /// </summary>
    [DataField]
    [Access(typeof(SharedGhostRoleSystem), Other = AccessPermissions.ReadWriteExecute)] // Don't make eye contact
    public List<EntProtoId> MindRoles = new() { "MindRoleGhostRoleNeutral" };

    [DataField]
    public bool AllowSpeech { get; set; } = true;

    [DataField]
    public bool AllowMovement { get; set; }

    [ViewVariables(VVAccess.ReadOnly)]
    public bool Taken { get; set; }

    [ViewVariables]
    public uint Identifier { get; set; }

    /// <summary>
    /// Reregisters the ghost role when the current player ghosts.
    /// </summary>
    [DataField("reregister")]
    public bool ReregisterOnGhost { get; set; } = true;

    /// <summary>
    /// If set, ghost role is raffled, otherwise it is first-come-first-serve.
    /// </summary>
    [DataField("raffle")]
    [Access(typeof(SharedGhostRoleSystem), Other = AccessPermissions.ReadWriteExecute)] // FIXME Friends
    public GhostRoleRaffleConfig? RaffleConfig { get; set; }

    /// <summary>
    /// Job the entity will receive after adding the mind.
    /// </summary>
    [DataField("job")]
    [Access(typeof(SharedGhostRoleSystem), Other = AccessPermissions.ReadWriteExecute)] // also FIXME Friends
    public ProtoId<JobPrototype>? JobProto = null;
}
