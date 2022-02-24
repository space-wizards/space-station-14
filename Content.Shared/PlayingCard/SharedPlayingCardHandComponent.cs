using Robust.Shared.Serialization;
using Robust.Shared.GameStates;

namespace Content.Shared.PlayingCard;



[Serializable]
[NetSerializable]
public sealed class PlayingCardHandBoundUserInterfaceState : BoundUserInterfaceState
{
    public List<string> CardList { get; }

    public PlayingCardHandBoundUserInterfaceState(List<string> cardList)
    {
        CardList = cardList;
    }
}

[Serializable, NetSerializable]
public class PickSingleCardMessage : BoundUserInterfaceMessage
{
    public readonly int ID;
    public PickSingleCardMessage(int id)
    {
        ID = id;
    }
}

[Serializable, NetSerializable]
public enum PlayingCardHandUiKey
{
    Key
}

[Serializable, NetSerializable]
public class CardListSyncRequestMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public class CardListMessage : BoundUserInterfaceMessage
{
    public readonly List<String> Cards;
    public CardListMessage(List<String> cards)
    {
        Cards = cards;
    }
}
