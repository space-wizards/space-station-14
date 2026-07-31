using Robust.Shared.Serialization;

namespace Content.Shared.Cards;

public abstract partial class SharedCardSystem
{
    [SubscribeLocalEvent]
    private void OnCardsStarted(Entity<CardsComponent> ent, ref ComponentStartup args)
    {
        UpdateVisualState(ent);
    }

    protected void UpdateVisualState(Entity<CardsComponent> ent)
    {
        if (TryComp<AppearanceComponent>(ent, out var appearance))
        {
            Appearance.SetData(ent, CardVisuals.CardList, GetCardListVisualState(ent.Comp), appearance);
            Appearance.SetData(ent, CardVisuals.IsFlipped, ent.Comp.Flipped, appearance);
        }
    }

    /// <summary>
    /// Builds the <see cref="CardListVisualState"/> describing which cards in the stack are currently
    /// visible to the player and should be rendered, based on whether the stack is fanned or flipped.
    /// </summary>
    /// <remarks>
    /// This determines what the client renders for the card sprite:
    /// if not fanned, only the top card is shown; if fanned, up to <see cref="CardsComponent.MaxFanned"/>
    /// cards are shown. If the stack is flipped, the visible window is taken from the end of the list
    /// instead of the start.
    /// </remarks>
    /// <param name="cards">The card stack component to compute the visual state for.</param>
    /// <returns>A <see cref="CardListVisualState"/> describing the visible slice of cards.</returns>
    public CardListVisualState GetCardListVisualState(CardsComponent cards)
    {
        var count = Math.Min(cards.Fanned ? cards.MaxFanned : 1, cards.Cards.Count) * (cards.Flipped ? 1 : -1);
        var start = cards.Flipped ? cards.Cards.Count - count : -count - 1;
        return new CardListVisualState
        {
            CardList = cards.Cards,
            Start = start,
            Count = count,
            MaxFanned = cards.MaxFanned,
        };
    }
}

[Serializable, NetSerializable]
public enum CardVisuals : byte
{
    IsFlipped,
    CardList,
}

[Serializable, NetSerializable]
public sealed class CardListVisualState : ICloneable
{
    public List<CardData> CardList = new();
    public int Start;
    public int Count;
    public int MaxFanned;

    public object Clone() => new CardListVisualState
    {
        CardList = CardList,
        Start = Start,
        Count = Count,
        MaxFanned = MaxFanned,
    };
}
