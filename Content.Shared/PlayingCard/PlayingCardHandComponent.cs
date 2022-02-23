using Robust.Shared.Serialization;
using Robust.Shared.GameStates;


namespace Content.Shared.PlayingCard
{
    [RegisterComponent]
    public sealed class PlayingCardHandComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("stackId")]
        public string StackTypeId { get; private set; } = string.Empty;

        [DataField("cardPrototype")]
        public string CardPrototype { get; private set; } = string.Empty;

        [DataField("cardList")]
        public List<string> CardList = new();
    }


        public struct CardDetails
        {
            public string Name;
            public string Description;

            public CardDetails(string name, string description)
            {
                Name = name;
                Description = description;
            }
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
