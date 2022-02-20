using Robust.Shared.Serialization;
using Robust.Shared.GameStates;


namespace Content.Shared.PlayingCard
{
    public abstract class SharedPlayingCardHandComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        // [DataField("stackType", required:true, customTypeSerializer:typeof(PrototypeIdSerializer<StackPrototype>))]
        [DataField("stackId")]
        public string StackTypeId { get; private set; } = string.Empty;

        [DataField("cardName")]
        public string CardName = "Playing Card";
        [DataField("cardDescription")]
        public string CardDescription = "a playing card";
        [DataField("cardList")]
        public List<String> CardList = new();
    }

        [Serializable, NetSerializable]
        public class PickSingleCardMessage : BoundUserInterfaceMessage
        {
            public readonly string ID;
            public PickSingleCardMessage(string id)
            {
                ID = id;
            }
        }

        [Serializable, NetSerializable]
        public enum PlayingCardHandUiKey
        {
            Key,
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

}
