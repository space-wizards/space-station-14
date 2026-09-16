using Content.Shared.Database;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared.Administration.Verbs.Prototypes;

/// <summary>
/// Defines a category containing related administrative entity verbs.
/// </summary>
[Prototype]
public sealed partial class AdminVerbCategoryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Localization key of the category name shown in the verb menu.
    /// </summary>
    [DataField(required: true)]
    public LocId Name { get; private set; }

    /// <summary>
    /// Texture path for the category icon.
    /// </summary>
    [DataField]
    public string? Icon { get; private set; }

    /// <summary>
    /// Display category entries as icons without their names.
    /// </summary>
    [DataField]
    public bool IconsOnly { get; private set; }

    /// <summary>
    /// Number of columns in the category menu. Use one when showing names.
    /// </summary>
    [DataField]
    public int Columns { get; private set; } = 1;

    /// <summary>
    /// All admin flags required to use entries in this category.
    /// </summary>
    [DataField]
    public AdminFlags RequiredFlags { get; private set; } = AdminFlags.Admin;

    /// <summary>
    /// Importance of entry execution in admin logs.
    /// </summary>
    [DataField]
    public LogImpact Impact { get; private set; } = LogImpact.Low;

    /// <summary>
    /// Required components for entries in this category.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist { get; private set; }

    /// <summary>
    /// Components that prevent entries in this category.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist { get; private set; }
}
