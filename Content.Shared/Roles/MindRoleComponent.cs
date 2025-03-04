using Content.Shared.Mind;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Roles;

/// <summary>
/// This holds data for, and indicates, a Mind Role entity
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MindRoleComponent : BaseMindRoleComponent
{
    /// <summary>
    ///     Marks this Mind Role as Antagonist
    ///     A single antag Mind Role is enough to make the owner mind count as Antagonist.
    /// </summary>
    [DataField]
    public bool Antag { get; set; } = false;

    /// <summary>
    ///     The mind's current antagonist/special role, or lack thereof;
    /// </summary>
    [DataField]
    public ProtoId<RoleTypePrototype>? RoleType;

    /// <summary>
    ///     True if this mindrole is an exclusive antagonist. Antag setting is not checked if this is True.
    /// </summary>
    [DataField]
    public bool ExclusiveAntag { get; set; } = false;

    /// <summary>
    ///     The Mind that this role belongs to
    /// </summary>
    public Entity<MindComponent> Mind { get; set; }

    /// <summary>
    ///     The Antagonist prototype of this role
    /// </summary>
    [DataField]
    public ProtoId<AntagPrototype>? AntagPrototype { get; set; }

    /// <summary>
    ///     The Job prototype of this role
    /// </summary>
    [DataField]
    public ProtoId<JobPrototype>? JobPrototype { get; set; }
}

// Why does this base component actually exist? It does make auto-categorization easy, but before that it was useless?
// I used it for easy organisation/bookkeeping of what components are for mindroles
[EntityCategory("Roles")]
public abstract partial class BaseMindRoleComponent : Component
{

}
