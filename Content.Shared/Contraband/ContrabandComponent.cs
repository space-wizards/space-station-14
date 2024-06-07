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
    public ContrabandSeverity Severity = ContrabandSeverity.Minor;
}

[Serializable, NetSerializable]
public enum ContrabandSeverity
{
    /// <summary>
    ///     Having this without a good reason might get you yelled at by security. (spears, shivs, etc)
    /// </summary>
    Minor,

    /// <summary>
    ///     Having this as a regular crew member, not the role it was made for, is considered theft IC. (rcd, sec gear, etc)
    /// </summary>
    RoleRestrictedTheft,

    /// <summary>
    ///     Having this as a regular crew member is considered grand theft. (nuke disk, cpatains gear, objective items, etc)
    /// </summary>
    GrandTheft,

    /// <summary>
    ///     This is clear syndicate contraband and is illegal to own IC.
    /// </summary>
    Syndicate
}
