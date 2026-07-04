using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Nutrition.Prototypes;

/// <summary>
/// This stores unique data for an item that is edible, such as verbs, verb icons, verb names, sounds, ect.
/// </summary>
[Prototype]
public sealed partial class EdiblePrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The sound we make when eaten.
    /// </summary>
    [DataField]
    public SoundSpecifier UseSound = new SoundCollectionSpecifier("eating");

    /// <summary>
    /// The localization identifier for the user's ingestion message.
    /// </summary>
    [DataField]
    public LocId Message;

    /// <summary>
    /// The localization identifier for an observer's or "others'" ingestion message.
    /// </summary>
    [DataField]
    public LocId OtherMessage;

    /// <summary>
    /// Localization verb used when consuming this item.
    /// </summary>
    [DataField]
    public LocId Verb;

    /// <summary>
    /// Localization noun used when consuming this item.
    /// </summary>
    [DataField]
    public LocId Noun;

    /// <summary>
    /// What type of food are we, currently used for determining verbs and some checks.
    /// </summary>
    [DataField]
    public LocId VerbName;

    /// <summary>
    /// What type of food are we, currently used for determining verbs and some checks.
    /// </summary>
    [DataField]
    public SpriteSpecifier? VerbIcon;


}
