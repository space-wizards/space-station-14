using Content.Shared.EntityEffects;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Administration.Verbs.Prototypes;

/// <summary>
/// Defines a target-filtered admin verb.
/// </summary>
[Prototype]
public sealed partial class AdminVerbPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Category containing this verb.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<AdminVerbCategoryPrototype> CategoryPrototype { get; private set; }

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
