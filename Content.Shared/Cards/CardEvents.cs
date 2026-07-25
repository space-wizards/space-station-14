using Robust.Shared.Serialization;

namespace Content.Shared.Cards;

/// <summary>
/// Raised by client to take specific card from a deck.
/// </summary>
[Serializable, NetSerializable]
public sealed class TakeCardEvent : EntityEventArgs
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
    /// The CardInx of the card to be taken.
    /// </summary>
    public readonly int CardInx;

    public TakeCardEvent(NetEntity cards, NetEntity user, int cardInx)
    {
        Cards = cards;
        User = user;
        CardInx = cardInx;
    }
}

/// <summary>
/// Raised by client to flip a deck.
/// </summary>
[Serializable, NetSerializable]
public sealed class FlipCardsEvent : EntityEventArgs
{
    /// <summary>
    /// The card deck to flip.
    /// </summary>
    public readonly NetEntity Cards;

    public FlipCardsEvent(NetEntity cards)
    {
        Cards = cards;
    }
}

/// <summary>
/// Raised by client to fan a deck.
/// </summary>
[Serializable, NetSerializable]
public sealed class FanCardsEvent : EntityEventArgs
{
    /// <summary>
    /// The card deck to fan out.
    /// </summary>
    public readonly NetEntity Cards;

    public FanCardsEvent(NetEntity cards)
    {
        Cards = cards;
    }
}

/// <summary>
/// Raised by client to shuffle a deck.
/// </summary>
[Serializable, NetSerializable]
public sealed class ShuffleCardsEvent : EntityEventArgs
{
    /// <summary>
    /// The card deck to shuffle.
    /// </summary>
    public readonly NetEntity Cards;

    public ShuffleCardsEvent(NetEntity cards)
    {
        Cards = cards;
    }
}
