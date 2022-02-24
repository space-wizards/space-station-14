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

        [ViewVariables]
        [DataField("stackId")]
        public string StackTypeId { get; private set; } = string.Empty;

        [ViewVariables]
        [DataField("cardPrototype")]
        public string CardPrototype { get; private set; } = string.Empty;

        [ViewVariables]
        [DataField("cardList")]
        public List<string> CardList = new();

    }
}
