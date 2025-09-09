using Content.Shared.Guidebook;
using Robust.Shared.Prototypes;
using Content.Shared.StatusIcon;

namespace Content.Shared.Roles;

    /// <summary>
    ///     Defines the abstract concept of a Role. A Role is a way of demarking a particular player as having
    ///     some particular "role" in the game, in the sense that they have some abilities and responsibilities,
    ///     rather than in the theatre sense. Examples include having a job or being an antagonist. A single player,
    ///     via their Mind, can have many Roles at once.
    /// </summary>
public abstract partial class RolePrototype : IPrototype
{
    /// <summary>
    ///     The ID of this Role. Required by IPrototype.
    /// </summary>
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     The name of this Role as displayed to players.
    /// </summary>
    [DataField]
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    ///     The requirements for this Role.
    /// </summary>
    [DataField, Access(typeof(SharedRoleSystem), Other = AccessPermissions.None)]
    public HashSet<JobRequirement>? Requirements;

    /// <summary>
    ///     Optional list of guides associated with this Role.
    ///     If the guides are opened, the first entry in this list
    ///     will be used to select the currently selected guidebook.
    /// </summary>
    [DataField]
    public List<ProtoId<GuideEntryPrototype>>? Guides;

    /// <summary>
    ///     Whether or not the player can set the Role in their preferences.
    /// </summary>
    [DataField]
    public bool SetPreference { get; private set; } = true;

    /// <summary>
    ///     The icon used for this Role when an icon needs to be displayed. Defaults to the icon for "unknown job" (typically a question mark).
    /// </summary>
    [DataField]
    public ProtoId<JobIconPrototype> Icon { get; private set; } = "JobIconUnknown";

    /// <summary>
    ///     Does this Role require being on an allowlist to use?
    /// </summary>
    [DataField]
    public bool Whitelisted;
}
