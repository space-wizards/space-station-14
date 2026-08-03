using System.Linq;
using System.Numerics;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Cards;

public abstract partial class SharedCardSystem : EntitySystem
{
    [Dependency] protected SharedHandsSystem Hands = default!;
    [Dependency] protected SharedPopupSystem Popup = default!;
    [Dependency] protected SharedAppearanceSystem Appearance = default!;
    [Dependency] protected SharedAudioSystem Audio = default!;
    [Dependency] protected SharedContainerSystem Container = default!;
    [Dependency] protected IGameTiming Timing = default!;
    [Dependency] protected SharedTransformSystem TransformSystem = default!;
    [Dependency] protected IPrototypeManager PrototypeManager = default!;
    [Dependency] protected ISharedPlayerManager PlayerManager = default!;

    [Dependency] private EntityQuery<CardsComponent> _cardsQuery;

    [SubscribeLocalEvent]
    protected virtual void OnCardsInit(Entity<CardsComponent> ent, ref ComponentInit args)
    {
        for (var i = 0; i < ent.Comp.Cards.Count; i++)
        {
            var card = ent.Comp.Cards[i];
            // Checks if this card has already been modified.
            // A card will only have a whitespace BaseState on initialization.
            if (
                !card.BaseState.IsWhiteSpace()
                || !PrototypeManager.Resolve(card.CardId, out var prototype)
            )
                continue;
            // Sets the card sprites to either the sprites set by the card or by the deck.
            card.BaseState = prototype.BaseState ?? ent.Comp.BaseState;
            card.CardBack = prototype.CardBack ?? ent.Comp.CardBack;
            ent.Comp.Cards[i] = card;
        }
    }

    private void MergeDecks(Entity<CardsComponent> donor, Entity<CardsComponent> recipient, List<CardData> selected)
    {
        MoveCards(recipient, donor, selected);

        UpdateVisualState(donor);
        UpdateVisualState(recipient);

        Dirty(donor);
        Dirty(recipient);

        if (donor.Comp.Cards.Count <= 0)
            PredictedQueueDel(donor.Owner);
    }

    [PublicAPI]
    public bool TryMergeDecks(
        Entity<CardsComponent?> donor,
        Entity<CardsComponent?> recipient,
        out int transferred,
        int? amount = null,
        List<int>? selected = null

    )
    {
        transferred = 0;

        if (donor.Owner == recipient.Owner)
            return false;

        // Recipient is being torn down, don't give it anything.
        if (TerminatingOrDeleted(recipient)
            || EntityManager.IsQueuedForDeletion(recipient))
            return false;

        // Check they're stacks of the same type
        if (!_cardsQuery.Resolve(recipient, ref recipient.Comp, false)
            || !_cardsQuery.Resolve(donor, ref donor.Comp, false)
            || recipient.Comp.CardStackType != donor.Comp.CardStackType)
            return false;

        // The most we can transfer
        transferred = Math.Min(donor.Comp.Cards.Count, GetAvailableSpace(recipient.Comp));
        if (transferred <= 0)
            return false;

        // transfer only as much as we want
        if (amount > 0)
            transferred = Math.Min(transferred, amount.Value);

        var cards = selected != null
            ? GetCardFromIndex(donor.Comp.Cards, selected)
            : GetCardFromIndex(donor.Comp.Cards, MovedCards(donor.Comp, transferred));

        if (selected != null && selected.Count != cards.Count)
            return false;

        MergeDecks((donor.Owner, donor.Comp), (recipient.Owner, recipient.Comp), cards);
        return true;
    }

    [PublicAPI]
    public int GetAvailableSpace(CardsComponent component)
    {
        return GetMaxCount(component) - component.Cards.Count;
    }

    [PublicAPI]
    public int GetMaxCount(CardsComponent component)
    {
        if (component.MaxCountOverride != null)
            return component.MaxCountOverride.Value;

        var cardStackProto = ProtoMan.Index(component.CardStackType);
        return cardStackProto.MaxCount ?? int.MaxValue;
    }

    public virtual EntityUid? SplitDeck(Entity<CardsComponent> ent, EntityCoordinates spawnPosition, List<int> cardIndexes = default!)
    {
        return null;
    }

    public void UserSplitDeck(Entity<CardsComponent> cards, EntityUid user, int amount)
    {
        if (amount <= 0)
        {
            Popup.PopupCursor(Loc.GetString("comp-stack-split-too-small"), user, PopupType.Medium);
            return;
        }

        // Tries to merge stack with a stack in hand.
        if (Hands.TryGetActiveItem(user, out var merger)
            && TryMergeDecks(cards.AsNullable(), merger.Value, out _, amount: amount))
        {
            Popup.PopupCursor(Loc.GetString("comp-stack-split"), user);
            return;
        }

        // If this is effectively just picking up the stack, it just picks up the stack.
        if (cards.Comp.Cards.Count <= amount)
        {
            Hands.PickupOrDrop(user, cards.Owner);
            return;
        }

        if (SplitDeck(cards, new EntityCoordinates(user, Vector2.Zero), MovedCards(cards.Comp, amount)) is not { } split)
            return;

        Hands.PickupOrDrop(user, split, animate: false);
        Popup.PopupCursor(Loc.GetString("comp-stack-split"), user);
    }

    [SubscribeLocalEvent]
    private void OnCardsContainerInserted(Entity<CardsComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        UpdateVisualState(ent);
        // Unfans cards put inside containers except hands
        if (ent.Comp.Fanned && !Hands.EnumerateHands(args.Container.Owner).Contains(args.Container.ID))
            TryFanCards(ent);
    }

    public bool TryMoveCards(Entity<CardsComponent> recipient, Entity<CardsComponent> donor, List<int> cardIndexes)
    {
        var selected = GetCardFromIndex(donor.Comp.Cards, cardIndexes);
        if (cardIndexes.Count != selected.Count)
            return false;
        MoveCards(recipient, donor, selected);
        return true;
    }

    protected void MoveCards(Entity<CardsComponent> recipient, Entity<CardsComponent> donor, List<CardData> selected)
    {
        // Remove cards from source
        foreach (var item in selected)
            donor.Comp.Cards.Remove(item);
        // Add cards to sink
        // The cards will be added to the side which is "facing upwards"
        if (recipient.Comp.Flipped)
            recipient.Comp.Cards.AddRange(selected);
        else
            recipient.Comp.Cards.InsertRange(0, selected);

        if (donor.Comp.Cards.Count == 1)
            donor.Comp.Fanned = false;

        if (donor.Comp.Cards.Count <= 0)
            PredictedQueueDel(donor.Owner);
    }

    public List<int> MovedCards(CardsComponent comp, int delta)
    {
        // Takes some number of cards from the top of the deck
        // Takes from the bottom if the deck is flipped
        if (comp.Flipped)
            return comp.Cards.TakeLast(delta).Select(c => c.CardIndex).ToList();
        return comp.Cards.Take(delta).Select(c => c.CardIndex).ToList();
    }

    [SubscribeNetworkEvent]
    private void HandleShuffleCardsEvent(ShuffleCardsEvent args)
    {
        var cards = GetEntity(args.Cards);
        if (_cardsQuery.TryComp(cards, out var comp))
            TryShuffleCards((cards, comp));
    }

    /// <summary>
    /// Attempts to shuffle the cards within the given <see cref="CardsComponent"/> into a random order.
    /// </summary>
    /// <param name="cards">The card stack entity to shuffle.</param>
    /// <returns><c>true</c> if the cards were shuffled. Currently always returns <c>true</c>.</returns>
    // Currently mis-predicted
    // TODO: FIX this mis-predict and replace with a proper animation
    public bool TryShuffleCards(Entity<CardsComponent> cards)
    {
        cards.Comp.Cards = cards.Comp.Cards.Shuffle().ToList();
        UpdateVisualState(cards);
        Dirty(cards.Owner, cards.Comp);
        return true;
    }

    [SubscribeNetworkEvent]
    private void HandleFlipCardsEvent(FlipCardsEvent args)
    {
        var cards = GetEntity(args.Cards);
        if (_cardsQuery.TryComp(cards, out var comp))
            TryFlipCards((cards, comp));
    }

    /// <summary>
    /// Attempts to flip the given card stack, toggling which side is face-up.
    /// </summary>
    /// <param name="cards">The card stack entity to flip.</param>
    /// <returns><c>true</c> if the cards were flipped. Currently always returns <c>true</c>.</returns>
    public bool TryFlipCards(Entity<CardsComponent> cards)
    {
        cards.Comp.Flipped = !cards.Comp.Flipped;
        UpdateVisualState(cards);
        Dirty(cards.Owner, cards.Comp);
        return true;
    }

    [SubscribeNetworkEvent]
    private void HandleFanCardsEvent(FanCardsEvent args)
    {
        var cards = GetEntity(args.Cards);
        if (_cardsQuery.TryComp(cards, out var comp))
            TryFanCards((cards, comp));
    }

    /// <summary>
    /// Attempts to toggle whether the given card stack is displayed fanned out.
    /// </summary>
    /// <param name="cards">The card stack entity to fan or unfan.</param>
    /// <returns><c>true</c> if the fan state was toggled. Currently always returns <c>true</c>.</returns>
    public bool TryFanCards(Entity<CardsComponent> cards)
    {
        cards.Comp.Fanned = !cards.Comp.Fanned;
        UpdateVisualState(cards);
        // Stack count updated so the deck below the fan shows the correct number of cards
        Dirty(cards.Owner, cards.Comp);
        return true;
    }

    [SubscribeNetworkEvent]
    private void HandleTakeCardEvent(TakeCardEvent args)
    {
        var cards = GetEntity(args.Cards);
        var user = GetEntity(args.User);
        if (_cardsQuery.TryComp(cards, out var comp))
            TryTakeCard((cards, comp), (user, Transform(user)), args.CardIndex, out _);
    }

    /// <summary>
    /// Attempts to take a specific card from a fanned stack and move it into the user's active hand,
    /// splitting the stack as needed.
    /// </summary>
    /// <param name="cards">The card stack entity to take a card from.</param>
    /// <param name="user">The entity attempting to take the card.</param>
    /// <param name="cardIndex">The index of the specific card being taken from the stack.</param>
    /// <param name="split">
    /// When this method returns, contains the entity that the split-off card(s) ended up on,
    /// or <c>null</c> if no split occurred or the operation failed.
    /// </param>
    /// <returns><c>true</c> if the card was successfully taken; otherwise <c>false</c>.</returns>
    public bool TryTakeCard(
        Entity<CardsComponent> cards,
        Entity<TransformComponent?> user,
        int cardIndex,
        out EntityUid? split
    )
    {
        split = null;
        if (!Resolve(user.Owner, ref user.Comp, false))
            return false;

        if (
            !Hands.TryGetActiveItem(user.Owner, out split)
            || !TryMergeDecks(cards.AsNullable(), (split.Value, null), out _, amount: 1, selected: new List<int> { cardIndex })
        )
        {
            split = SplitDeck(cards, user.Comp.Coordinates, new List<int> { cardIndex });
            if (split == null)
                return false;
        }
        if (!_cardsQuery.TryComp(split, out var recipientStack))
            return false;

        // If this is true it is a new deck so copies over the properties
        // Otherwise it doesn't change the deck the card joins
        if (recipientStack.Cards.Count == 1)
        {
            recipientStack.Flipped = cards.Comp.Flipped;
            recipientStack.Fanned = cards.Comp.Fanned;
            Hands.PickupOrDrop(user.Owner, split.Value);
        }

        Popup.PopupCursor(Loc.GetString("comp-stack-split"), user.Owner);

        UpdateVisualState(cards);
        UpdateVisualState((split.Value, recipientStack));

        Dirty(cards.Owner, cards.Comp);
        Dirty(split.Value, recipientStack);

        return true;
    }

    /// <summary>
    /// Finds the <see cref="CardData"/> in the given list whose card index matches the specified value.
    /// </summary>
    /// <param name="cards">The list of cards to search.</param>
    /// <param name="cardIndex">The card index to search for.</param>
    /// <returns>The matching <see cref="CardData"/>, or <c>null</c> if no card with that index exists.</returns>
    public CardData? GetCardFromIndex(List<CardData> cards, int cardIndex)
    {
        var card = cards.Find(c => c.CardIndex == cardIndex);
        return card.CardId.Id == null ? null : card;
    }

    public List<CardData> GetCardFromIndex(List<CardData> cards, List<int> cardIndexes)
    {
        return cards.Where(c => cardIndexes.Contains(c.CardIndex)).ToList();
    }

}
