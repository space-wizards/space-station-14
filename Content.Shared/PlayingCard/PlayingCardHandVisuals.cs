using Robust.Shared.Serialization;

namespace Content.Shared.PlayingCard
{
    [Serializable, NetSerializable]
    public enum PlayingCardHandVisuals : byte
    {
        CardList,
    }

    [Serializable, NetSerializable]
     public sealed class CardListVisualState : ICloneable
    {
        // These should be the last 5 cards for their relevant state
        public readonly List<string> CardList;
        public readonly string PlayingCardContentPrototypeID;
        public CardListVisualState(List<string> cardList, string playingCardContentPrototypeID)
        {
            CardList = cardList;
            PlayingCardContentPrototypeID = playingCardContentPrototypeID;
        }
        public object Clone()
        {
            return new CardListVisualState(CardList, PlayingCardContentPrototypeID);
        }
    }
}
