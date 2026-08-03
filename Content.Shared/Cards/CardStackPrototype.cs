using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Utility;

namespace Content.Shared.Cards;

/// <summary>
/// Prototype used to combine and split decks of cards
/// </summary>
[Prototype]
public sealed partial class CardStackPrototype : IPrototype, IInheritingPrototype
{
    ///  <inheritdoc />
    [IdDataField]
    public string ID { get; private set; } = default!;

    ///  <inheritdoc />
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<CardStackPrototype>))]
    public string[]? Parents { get; private set; }

    ///  <inheritdoc />
    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }

    /// <summary>
    /// Human-readable name for this stack type e.g. "Deck of Cards"
    /// Will overwrite initial entity name after splitting
    /// </summary>
    /// <remarks>This is a localization string ID.</remarks>
    [DataField]
    public LocId Name { get; private set; } = string.Empty;

    /// <summary>
    /// An icon that will be used to represent this stack type.
    /// </summary>
    [DataField]
    public SpriteSpecifier? Icon { get; private set; }

    /// <summary>
    /// The entity id that will be spawned by default from this deck.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId<CardsComponent> Spawn { get; private set; }

    [DataField]
    public int? MaxCount { get; private set; }
}
