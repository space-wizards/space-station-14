using Content.Shared.Ghost.Roles.Raffles;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared.Ghost.Roles.Components;

[RegisterComponent]
[Access(typeof(SharedGhostRoleSystem))]
public sealed partial class GhostRoleComponent : Component
{
    /// <summary>
    /// Localized name shown for this ghost role.
    /// </summary>
    [DataField("name")]
    [Access(typeof(SharedGhostRoleSystem), Other = AccessPermissions.ReadWriteExecute)]
    public LocId _roleName = "generic-unknown-title";

    /// <summary>
    /// Localized description shown in the ghost role UI.
    /// </summary>
    [DataField("description")]
    [Access(typeof(SharedGhostRoleSystem), Other = AccessPermissions.ReadWriteExecute)]
    public LocId _roleDescription = "generic-unknown-title";

    /// <summary>
    /// Localized rules text shown before taking the role.
    /// </summary>
    [DataField("rules")]
    [Access(typeof(SharedGhostRoleSystem), Other = AccessPermissions.ReadWriteExecute)]
    public LocId _roleRules = "ghost-role-component-default-rules";

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

    [ViewVariables(VVAccess.ReadWrite)]
    [Access(typeof(SharedGhostRoleSystem), Other = AccessPermissions.ReadWriteExecute)] // FIXME Friends
    public string RoleName
    {
        get => Loc.GetString(_roleName);
        set
        {
            _roleName = value;
            IoCManager.Resolve<IEntityManager>().System<SharedGhostRoleSystem>().UpdateAllEui();
        }
    }

    [ViewVariables(VVAccess.ReadWrite)]
    [Access(typeof(SharedGhostRoleSystem), Other = AccessPermissions.ReadWriteExecute)] // FIXME Friends
    public string RoleDescription
    {
        get => Loc.GetString(_roleDescription);
        set
        {
            _roleDescription = value;
            IoCManager.Resolve<IEntityManager>().System<SharedGhostRoleSystem>().UpdateAllEui();
        }
    }

    [ViewVariables(VVAccess.ReadWrite)]
    [Access(typeof(SharedGhostRoleSystem), Other = AccessPermissions.ReadWriteExecute)] // FIXME Friends
    public string RoleRules
    {
        get => Loc.GetString(_roleRules);
        set
        {
            _roleRules = value;
            IoCManager.Resolve<IEntityManager>().System<SharedGhostRoleSystem>().UpdateAllEui();
        }
    }

    /// <summary>
    /// The mind roles that will be added to the mob's mind entity
    /// </summary>
    [DataField]
    [Access(typeof(SharedGhostRoleSystem), Other = AccessPermissions.ReadWriteExecute)] // Don't make eye contact
    public List<EntProtoId> MindRoles = new() { "MindRoleGhostRoleNeutral" };

    /// <summary>
    /// Whether the granted mob is allowed to speak after takeover.
    /// </summary>
    [DataField]
    public bool AllowSpeech { get; set; } = true;

    /// <summary>
    /// Whether the granted mob is allowed to move after takeover.
    /// </summary>
    [DataField]
    public bool AllowMovement { get; set; }

    /// <summary>
    /// Whether this role has already been taken and should no longer be available.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public bool Taken { get; set; }

    /// <summary>
    /// Runtime identifier used by UI requests and raffle tracking.
    /// </summary>
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
    /// Job role added to the entity's mind after takeover.
    /// </summary>
    [DataField("job")]
    [Access(typeof(SharedGhostRoleSystem), Other = AccessPermissions.ReadWriteExecute)] // also FIXME Friends
    public ProtoId<JobPrototype>? JobProto = null;
}
