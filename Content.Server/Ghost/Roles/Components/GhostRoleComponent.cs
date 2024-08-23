using Content.Server.Ghost.Roles.Raffles;
using Content.Server.Mind.Commands;
using Content.Shared.Roles;

namespace Content.Server.Ghost.Roles.Components;

[RegisterComponent]
[Access(typeof(GhostRoleSystem))]
public sealed partial class GhostRoleComponent : Component
{
    [DataField("name")] private string _roleName = "Unknown";

    [DataField("description")] private string _roleDescription = "Unknown";

    [DataField("rules")] private string _roleRules = "ghost-role-component-default-rules";

    // TODO ROLE TIMERS
    // Actually make use of / enforce this requirement?
    // Why is this even here.
    // Move to ghost role prototype & respect CCvars.GameRoleTimerOverride
    [DataField("requirements")]
    public HashSet<JobRequirement>? Requirements;

    /// <summary>
    /// Whether the <see cref="MakeSentientCommand"/> should run on the mob.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)] [DataField("makeSentient")]
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

    [DataField("allowSpeech")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool AllowSpeech { get; set; } = true;

    [DataField("allowMovement")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool AllowMovement { get; set; }

    [ViewVariables(VVAccess.ReadOnly)]
    public bool Taken { get; set; }

    [ViewVariables]
    public uint Identifier { get; set; }

    /// <summary>
    /// Reregisters the ghost role when the current player ghosts.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("reregister")]
    public bool ReregisterOnGhost { get; set; } = true;

    /// <summary>
    /// If set, ghost role is raffled, otherwise it is first-come-first-serve.
    /// </summary>
    [DataField("raffle")]
    [Access(typeof(GhostRoleSystem), Other = AccessPermissions.ReadWriteExecute)] // FIXME Friends
    public GhostRoleRaffleConfig? RaffleConfig { get; set; }
}
