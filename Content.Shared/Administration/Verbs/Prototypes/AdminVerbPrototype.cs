using Content.Shared.Database;
using Content.Shared.EntityEffects;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Utility;

namespace Content.Shared.Administration.Verbs.Prototypes;

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

    /// <summary>
    /// Category containing this verb.
    /// </summary>
    [DataField]
    public ProtoId<AdminVerbCategoryPrototype>? CategoryPrototype { get; private set; }

    /// <summary>
    /// Localization key of the verb name shown in the admin verb menu.
    /// </summary>
    [DataField(required: true)]
    public LocId Name { get; private set; }

    /// <summary>
    /// Localization key of the verb description shown in the verb menu.
    /// </summary>
    [DataField]
    public LocId? Description { get; private set; }

    /// <summary>
    /// Icon shown next to the verb in the admin verb menu.
    /// </summary>
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

    /// <summary>
    /// Required components for the verb.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist { get; private set; }

    /// <summary>
    /// Components that prevent the verb.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist { get; private set; }

    /// <summary>
    /// Effects applied to the target when the verb is used.
    /// </summary>
    [DataField]
    public EntityEffect[] Effects { get; private set; } = [];
}
