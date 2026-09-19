using Robust.Shared.Serialization;

namespace Content.Shared.Cards;

/// <summary>
/// Raised by client to take specific card from a deck.
/// </summary>
[Serializable, NetSerializable]
public record struct TakeCardEvent
{
    /// <summary>
    /// The card deck the card is being taken from.
    /// </summary>
    public readonly NetEntity Cards;

    /// <summary>
    /// The entity attempting to take the card.
    /// </summary>
    public readonly NetEntity User;

    /// <summary>
    /// The CardIndex of the card to be taken.
    /// </summary>
    public readonly int CardIndex;
}

/// <summary>
/// Raised by client to flip a deck.
/// </summary>
[Serializable, NetSerializable]
public record struct FlipCardsEvent
{
    /// <summary>
    /// The card deck to flip.
    /// </summary>
    public readonly NetEntity Cards;
}

/// <summary>
/// Raised by client to fan a deck.
/// </summary>
[Serializable, NetSerializable]
public record struct FanCardsEvent
{
    /// <summary>
    /// The card deck to fan out.
    /// </summary>
    public readonly NetEntity Cards;
}

/// <summary>
/// Raised by client to shuffle a deck.
/// </summary>
[Serializable, NetSerializable]
public record struct ShuffleCardsEvent
{
    /// <summary>
    /// The card deck to shuffle.
    /// </summary>
    public readonly NetEntity Cards;
}
