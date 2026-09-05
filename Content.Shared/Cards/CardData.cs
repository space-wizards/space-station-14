using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Cards;

/// <summary>
/// Stores the data for an individual card
/// </summary>
[DataRecord, NetSerializable, Serializable]
public partial struct CardData
{
    /// <summary>
    /// Prototype for the card with sprites.
    /// </summary>
    public ProtoId<CardPrototype> CardId = string.Empty;

    /// <summary>
    /// The sprite for the base layer of the cards in the deck; is set on card init.
    /// </summary>
    public string BaseState = string.Empty;

    /// <summary>
    /// The sprite for the back layer of the cards; is set on card init.
    /// </summary>
    public string CardBack = string.Empty;

    /// <summary>
    /// Unique index to this specific card. Used for server and client agreement when picking cards.
    /// It is not an index within a deck.
    /// </summary>
    public int CardIndex;
}
