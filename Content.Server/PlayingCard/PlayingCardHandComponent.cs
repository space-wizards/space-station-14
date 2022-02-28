using Content.Shared.PlayingCard;
using Content.Server.UserInterface;
using Robust.Server.GameObjects;


namespace Content.Server.PlayingCard
{
    [RegisterComponent]
    public sealed class PlayingCardHandComponent : Component
    {
        [ViewVariables]
        public BoundUserInterface? UserInterface => Owner.GetUIOrNull(PlayingCardHandUiKey.Key);

        public string CardDeckID = string.Empty;

        [ViewVariables]
        [DataField("cardPrototype")]
        public string CardPrototype = string.Empty;

        [ViewVariables]
        [DataField("cardList")]
        public List<string> CardList = new();

    }
}
