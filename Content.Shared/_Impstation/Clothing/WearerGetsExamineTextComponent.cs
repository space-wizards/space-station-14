using Robust.Shared.GameStates;

namespace Content.Shared._Impstation.Clothing;

/// <summary>
/// Adds examine text to the entity that wears item, for making things obvious.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(WearerGetsExamineTextSystem))]
public sealed partial class WearerGetsExamineTextComponent : Component
{
    /// <summary>
    /// The LocId that specifies what category of object this is.
    /// i.e. "pin" or "scarf"
    /// Should be redefined on a per-category basis, naturally.
    /// </summary>
    [DataField("thing")]
    public LocId Category = "obvious-thing-default";

    /// <summary>
    /// The LocId that specifies what member of the category this is.
    /// i.e. "lesbian pride"
    /// Can be used to define text colors that are copied to all things
    /// which share this specifier (i.e. the other items of the same pride).
    /// (And summarily, makes accessibility-based changes for these colors a cinch.)
    /// Should be defined by each thing that has this component.
    /// </summary>
    [DataField("thingType")]
    public LocId Specifier = "obvious-type-default";

    /// <summary>
    /// The LocId that will be added to any wearing entity's examination.
    /// Typically only needs redefining on a per-category basis,
    /// but items that should have totally-unique obvious text can simply specify them here.
    /// </summary>
    [DataField("examineText", required: true)]
    public LocId ExamineOnWearer = "obvious-desc-default";

    /// <summary>
    /// Reference to the entity wearing this clothing.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Wearer;
    /// <summary>
    /// The string that is attached to this item's ExamineOnWearer.
    /// Typically doesn't need to be redefined.
    /// </summary>
    [DataField]
    public LocId PrefixExamineOnWearer = "obvious-prefix-wearing";

    /// <summary>
    /// If true, an entity with this item in any slot (i.e. in pockets) will gain the examine text,
    /// instead of when just equipped as clothing.
    /// Should be used sparingly only when truly appropriate; this is effectively a half-measure for lack of a special pin slot.
    /// </summary>
    [DataField]
    public bool PocketEvident;

    /// <summary>
    /// If true, the entity's description will inform examiners what others will see on the wearer (before they equip it).
    /// If the item is contraband, the item will also warn that displaying it may cause undue attention.
    /// Keep this false for good-natured jokes (i.e. the pride cloaks having funny, non-pride names)
    /// </summary>
    [DataField]
    public bool WarnExamine = true;
}
