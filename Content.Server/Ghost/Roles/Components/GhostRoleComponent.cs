using Content.Server.Ghost.Roles.Raffles;
using Content.Server.Mind.Commands;
using Content.Shared.Antag;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.Ghost.Roles.Components;

[RegisterComponent]
[Access(typeof(GhostRoleSystem))]
public sealed partial class GhostRoleComponent : Component
{
    [DataField("name")] private string _roleName = "Unknown";

    [DataField("description")] private string _roleDescription = "Unknown";

    [DataField("rules")] private string _roleRules = "ghost-role-component-default-rules";

    /// <summary>
    /// Whether the <see cref="MakeSentientCommand"/> should run on the mob.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)][DataField("makeSentient")]
    public bool MakeSentient = true;

    /// <summary>
    ///     The probability that this ghost role will be available after init.
    ///     Used mostly for takeover roles that want some probability of being takeover, but not 100%.
    /// </summary>
    [DataField("prob")]
    public float Probability = 1f;

    // We do this so updating RoleName and RoleDescription in VV updates the open EUIs.

    [ViewVariables(VVAccess.ReadWrite)]
    [Access(typeof(GhostRoleSystem), Other = AccessPermissions.ReadWriteExecute)] // FIXME Friends
    public string RoleName
    {
        get => Loc.GetString(_roleName);
        set
        {
            _roleName = value;
            IoCManager.Resolve<IEntityManager>().System<GhostRoleSystem>().UpdateAllEui();
        }
    }

    [ViewVariables(VVAccess.ReadWrite)]
    [Access(typeof(GhostRoleSystem), Other = AccessPermissions.ReadWriteExecute)] // FIXME Friends
    public string RoleDescription
    {
        get => Loc.GetString(_roleDescription);
        set
        {
            _roleDescription = value;
            IoCManager.Resolve<IEntityManager>().System<GhostRoleSystem>().UpdateAllEui();
        }
    }

    [ViewVariables(VVAccess.ReadWrite)]
    [Access(typeof(GhostRoleSystem), Other = AccessPermissions.ReadWriteExecute)] // FIXME Friends
    public string RoleRules
    {
        get => Loc.GetString(_roleRules);
        set
        {
            _roleRules = value;
            IoCManager.Resolve<IEntityManager>().System<GhostRoleSystem>().UpdateAllEui();
        }
    }

    /// <summary>
    /// If not null, the player will become the antagonist
    /// Does not add components, StartingGear, and RoleLoadout if it is not a spawner <see cref="GhostRoleMobSpawnerComponent"/>
    /// </summary>
    [DataField, Access(typeof(GhostRoleSystem), Other = AccessPermissions.ReadWriteExecute)]
    public ProtoId<AntagLoadoutPrototype>? AntagLoadoutPrototype;

    [DataField]
    public bool AllowSpeech = true;

    [DataField]
    public bool AllowMovement;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool Taken;

    [ViewVariables]
    public uint Identifier;

    /// <summary>
    /// If true, adds a goal with the obedience of a specific player. The owner is selected by other components
    /// </summary>
    [DataField, Access(typeof(GhostRoleSystem), Other = AccessPermissions.ReadWriteExecute)]
    public bool Minion = false;

    /// <summary>
    /// Master of the minion.
    /// </summary>
    [DataField, Access(typeof(GhostRoleSystem), Other = AccessPermissions.ReadWriteExecute)]
    public EntityUid? Master;

    /// <summary>
    /// The objective of submission
    /// </summary>
    [DataField, Access(typeof(GhostRoleSystem), Other = AccessPermissions.ReadWriteExecute)]
    public EntProtoId MinionSubmissionObjective = "MinionSubmissionObjective";

    /// <summary>
    /// Reregisters the ghost role when the current player ghosts.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("reregister")]
    public bool ReregisterOnGhost = true;

    /// <summary>
    /// If set, ghost role is raffled, otherwise it is first-come-first-serve.
    /// </summary>
    [DataField("raffle")]
    [Access(typeof(GhostRoleSystem), Other = AccessPermissions.ReadWriteExecute)] // FIXME Friends
    public GhostRoleRaffleConfig? RaffleConfig;

    /// <summary>
    /// Job the entity will receive after adding the mind.
    /// </summary>
    [DataField("job")]
    [Access(typeof(GhostRoleSystem), Other = AccessPermissions.ReadWriteExecute)] // also FIXME Friends
    public ProtoId<JobPrototype>? JobProto = null;
}

