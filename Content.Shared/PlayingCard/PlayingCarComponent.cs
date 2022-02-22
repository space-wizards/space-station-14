namespace Content.Shared.PlayingCard
{
    [RegisterComponent]
    public sealed class PlayingCardComponent : Component
    {
        public string StackTypeId { get; private set; } = string.Empty;

        [DataField("cardName")]
        public string CardName = "playing card";

        [DataField("cardDescription")]

        public string CardDescription = "A playing card";
    }
}
