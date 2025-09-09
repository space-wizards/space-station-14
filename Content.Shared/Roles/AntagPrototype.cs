using Robust.Shared.Prototypes;

namespace Content.Shared.Roles;

/// <summary>
///     Describes information for a single antag.
/// </summary>
[Prototype]
public sealed partial class AntagPrototype : RolePrototype
{
    /// <summary>
    ///     The group name of all antagonists when they need to be grouped together in a dictionary of lists of roles.
    ///     Equivalent to Jobs using a DepartmentPrototype ID.
    /// </summary>
    ///
    public static readonly string GroupName = "Antagonist";

    /// <summary>
    ///     The color that all antagonists use when they visually grouped together.
    ///     Equivalent to DepartmentPrototype's Color property.
    /// </summary>
    public static readonly Color GroupColor = Color.Red;

    /// <summary>
    ///     The antag's objective, shown in a tooltip in the antag preference menu or as a ghost role description.
    /// </summary>
    [DataField]
    public string Objective { get; private set; } = "";

    /// <summary>
    ///     Whether the antag role is one of the bad guys.
    /// </summary>
    [DataField]
    public bool Antagonist { get; private set; }
}
