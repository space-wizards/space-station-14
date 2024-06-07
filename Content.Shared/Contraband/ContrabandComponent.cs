using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Contraband;

/// <summary>
/// This is used for marking entities that are considered 'contraband' IC and showing it clearly in examine.
/// </summary>
[RegisterComponent]
public sealed partial class ContrabandComponent : Component
{
    /// <summary>
    ///     The degree of contraband severity this item is considered to have.
    /// </summary>
    [DataField]
    public ContrabandSeverity Severity = ContrabandSeverity.Restricted;

    /// <summary>
    ///     Which departments is this item restricted to?
    ///     By default, command and sec are assumed to be fine with contraband.
    ///     If null, no departments are allowed to use this.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<DepartmentPrototype>>? AllowedDepartments = ["Security"];
}

[Serializable, NetSerializable]
public enum ContrabandSeverity
{
    /// <summary>
    ///     Improvised weapons/gear, etc. Not departmentally restricted per se, but you shouldn't really have it around
    ///     as non-sec without a valid reason.
    /// </summary>
    Minor,

    /// <summary>
    ///     Having this without a good reason might get you yelled at by security. (spears, shivs, etc).
    ///     or, Having this as a regular crew member, not the department it was made for, is considered theft IC. (rcd, sec gear, etc)
    /// </summary>
    Restricted,

    /// <summary>
    ///     Having this as a regular crew member is considered grand theft. (nuke disk, cpatains gear, objective items, etc)
    /// </summary>
    GrandTheft,

    /// <summary>
    ///     This is clear syndicate contraband and is illegal to own IC.
    /// </summary>
    Syndicate
}
