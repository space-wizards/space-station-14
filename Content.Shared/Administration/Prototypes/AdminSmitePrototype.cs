using Content.Shared.EntityEffects;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Administration.Prototypes;

/// <summary>
/// Special effects that the administration can apply to various entities
/// </summary>
[Prototype("adminSmite")]
public sealed partial class AdminSmitePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The name that will be displayed in the Verbs menu
    /// </summary>
    [DataField(required: true)]
    public LocId Name = string.Empty;

    /// <summary>
    /// The description that will be displayed in the Verbs menu
    /// </summary>
    [DataField]
    public LocId Desc = string.Empty;

    /// <summary>
    /// This message will appear above the entity to which smite is applied. If null, there will be no message.
    /// </summary>
    [DataField]
    public LocId? PopupMessage;

    /// <summary>
    /// Determines which entities this smite can be applied to.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Icon displayed in the Verbs menu
    /// </summary>
    [DataField(required: true)]
    public SpriteSpecifier? Icon;

    [DataField]
    public List<EntityEffect> Effects = new();

    [DataField]
    public ComponentRegistry Components = new();
}
