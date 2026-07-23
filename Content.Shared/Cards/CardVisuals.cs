using Content.Shared.Stacks;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Cards;

public abstract partial class SharedCardSystem
{
    [SubscribeLocalEvent]
    private void OnCardsStarted(Entity<CardsComponent> ent, ref ComponentStartup args)
    {
        UpdateVisualState(ent);
    }

    [SubscribeLocalEvent]
    private void OnStackCountChanged(Entity<CardsComponent> ent, ref StackCountChangedEvent args)
    {
        // Makes sure the sudo stack count for visuals is up to date
        if (args.NewCount == 0)
            return;
        UpdateStackCount(ent);
    }

    private void UpdateStackCount(Entity<CardsComponent> ent)
    {
        // If the deck is fanned it changes the visual count to what ever number is below the fanned cards
        // This means that a deck with the same number of cards as the MaxFanned will not have a stack extending of the cards
        var visualState = GetCardListVisualState(ent.Comp);
        Appearance.SetData(ent.Owner, StackVisuals.Actual, ent.Comp.Cards.Count - visualState.Count + 1);
    }

    private void UpdateVisualState(Entity<CardsComponent> ent)
    {
        UpdateStackCount(ent);
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
        var count = Math.Min(cards.Fanned ? cards.MaxFanned : 1, cards.Cards.Count);
        var start = cards.Flipped ? cards.Cards.Count - count : 0;
        return new CardListVisualState(cards.Cards, start, count);
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
    public List<CardData> CardList;
    public int Start;
    public int Count;

    public CardListVisualState(List<CardData> cardList, int start, int count)
    {
        CardList = cardList;
        Start = start;
        Count = count;
    }

    public object Clone() => new CardListVisualState(CardList, Start, Count);
}
