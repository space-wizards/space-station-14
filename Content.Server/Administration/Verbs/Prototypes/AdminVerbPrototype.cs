using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.EntityEffects;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Utility;

namespace Content.Server.Administration.Verbs.Prototypes;

/// <summary>
/// Defines a target-filtered admin verb.
/// </summary>
[Prototype]
public sealed partial class AdminVerbPrototype : IPrototype, IInheritingPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<AdminVerbPrototype>))]
    public string[]? Parents { get; private set; }

    [AbstractDataField, NeverPushInheritance]
    public bool Abstract { get; private set; }

    [DataField(required: true)]
    public LocId Name { get; private set; }

    [DataField]
    public LocId? Description { get; private set; }

    [DataField]
    public SpriteSpecifier? Icon { get; private set; }

    /// <summary>
    /// All admin flags required to use this verb.
    /// </summary>
    [DataField]
    public AdminFlags RequiredFlags { get; private set; } = AdminFlags.Admin;

    /// <summary>
    /// Importance of the verb execution in admin logs.
    /// </summary>
    [DataField]
    public LogImpact Impact { get; private set; } = LogImpact.Low;

    /// <summary>
    /// Localized category name. Null leaves the verb outside a category.
    /// </summary>
    [DataField]
    public LocId? Category { get; private set; }

    /// <summary>
    /// Texture path for the category icon.
    /// </summary>
    [DataField]
    public string? CategoryIcon { get; private set; }

    /// <summary>
    /// Display category entries as icons without their names.
    /// </summary>
    [DataField]
    public bool CategoryIconsOnly { get; private set; }

    /// <summary>
    /// Number of columns in the category menu. Use one when showing names.
    /// </summary>
    [DataField]
    public int CategoryColumns { get; private set; } = 1;

    [DataField]
    public EntityWhitelist? Whitelist { get; private set; }

    [DataField]
    public EntityWhitelist? Blacklist { get; private set; }

    [DataField]
    public EntityEffect[] Effects { get; private set; } = [];
}
