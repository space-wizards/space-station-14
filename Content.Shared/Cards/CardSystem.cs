using System.Linq;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Cards;

public abstract partial class SharedCardSystem : EntitySystem
{
    [Dependency] protected SharedStackSystem Stacks = default!;
    [Dependency] protected SharedHandsSystem Hands = default!;
    [Dependency] protected SharedPopupSystem Popup = default!;
    [Dependency] protected SharedAppearanceSystem Appearance = default!;
    [Dependency] protected SharedAudioSystem Audio = default!;
    [Dependency] protected SharedContainerSystem Container = default!;
    [Dependency] protected IGameTiming Timing = default!;
    [Dependency] protected SharedTransformSystem TransformSystem = default!;
    [Dependency] protected IPrototypeManager PrototypeManager = default!;
    [Dependency] protected ISharedPlayerManager PlayerManager = default!;

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
                || !PrototypeManager.TryIndex<CardPrototype>(card.CardId, out var prototype)
            )
                continue;
            // Sets the card sprites to either the sprites set by the card or by the deck.
            card.BaseState = prototype.BaseState == null ? ent.Comp.BaseState : prototype.BaseState;
            card.CardBack = prototype.CardBack == null ? ent.Comp.CardBack : prototype.CardBack;
            ent.Comp.Cards[i] = card;
        }
    }

    // Whenever stacks are merged.
    [SubscribeLocalEvent]
    private void OnMergeEvent(Entity<CardsComponent> ent, ref StackMergeEvent args)
    {
        if (!TryComp<CardsComponent>(args.Donor, out var donorComp))
            return;
        // If BeingCherryPicked the merging is dealt with elsewhere
        if (ent.Comp.BeingCherryPicked || donorComp.BeingCherryPicked)
            return;

        TakeFromDeck(ent.Comp, donorComp, args.Amount);
        UpdateVisualState(ent);
        UpdateVisualState((args.Donor, donorComp));

        Dirty(ent.Owner, ent.Comp);
        Dirty(args.Donor, donorComp);
    }

    // Whenever stacks are split.
    [SubscribeLocalEvent]
    private void OnSplitEvent(Entity<CardsComponent> ent, ref StackSplitEvent args)
    {
        if (ent.Comp.BeingCherryPicked)
            return;
        if (
            !TryComp<CardsComponent>(args.NewId, out var splitComp)
            || !TryComp<StackComponent>(args.NewId, out var splitStackComp)
        )
            return;

        var delta = splitStackComp.Count;

        TakeFromDeck(splitComp, ent.Comp, delta);
        // Copy state over to new entity
        splitComp.Flipped = ent.Comp.Flipped;
        splitComp.Fanned = ent.Comp.Fanned;

        UpdateVisualState(ent);
        UpdateVisualState((args.NewId, splitComp));

        Dirty(ent.Owner, ent.Comp);
        Dirty(args.NewId, splitComp);
    }

    [SubscribeLocalEvent]
    private void OnCardsContainerInserted(Entity<CardsComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        UpdateVisualState(ent);
        // Unfans cards put inside containers except hands
        if (ent.Comp.Fanned && !Hands.EnumerateHands(args.Container.Owner).Contains(args.Container.ID))
            TryFanCards(ent);
    }

    private void TakeFromDeck(CardsComponent comp1, CardsComponent comp2, int delta)
    {
        // Takes cards from the top or bottom of deck depending on how it is flipped
        var selected = MovedCards(comp2, delta);
        MoveCards(comp1, comp2, selected);
    }

    private void MoveCards(CardsComponent comp1, CardsComponent comp2, List<CardData> selected)
    {
        // Remove cards from source
        foreach (var item in selected)
            comp2.Cards.Remove(item);
        // Add cards to sink
        // The cards will be added to the side which is "facing upwards"
        if (comp1.Flipped)
            comp1.Cards.AddRange(selected);
        else
            comp1.Cards.InsertRange(0, selected);

        if (comp2.Cards.Count == 1)
            comp2.Fanned = false;
    }

    private List<CardData> MovedCards(CardsComponent comp, int delta)
    {
        // Takes some number of cards from the top of the deck
        // Takes from the bottom if the deck is flipped
        if (comp.Flipped)
            return comp.Cards.TakeLast(delta).ToList();
        return comp.Cards.Take(delta).ToList();
    }

    [SubscribeNetworkEvent]
    private void HandleShuffleCardsEvent(ShuffleCardsEvent args)
    {
        var cards = GetEntity(args.Cards);
        if (TryComp<CardsComponent>(cards, out var comp))
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
        if (TryComp<CardsComponent>(cards, out var comp))
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
        if (TryComp<CardsComponent>(cards, out var comp))
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
        if (TryComp<CardsComponent>(cards, out var comp))
            TryTakeCard((cards, comp), (user, Transform(user)), args.CardInx, out _);
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
        if (!Resolve(user.Owner, ref user.Comp, false) || !TryComp<StackComponent>(cards.Owner, out var stackComp))
            return false;

        // Card movement needs to be a specific card so this prevents the merge or split event from taking from top of deck
        cards.Comp.BeingCherryPicked = true;

        // This section is effectively SharedStackSystem.UserSplit()
        if (
            !Hands.TryGetActiveItem(user.Owner, out split)
            || !TryComp<StackComponent>(split, out var recipientStack)
            || !Stacks.TryMergeStacks((cards.Owner, stackComp), (split.Value, recipientStack), out _, amount: 1)
        )
        {
            split = Stacks.Split((cards.Owner, stackComp), 1, user.Comp.Coordinates);
            if (split == null)
            {
                cards.Comp.BeingCherryPicked = false;
                return false;
            }
        }
        cards.Comp.BeingCherryPicked = false;

        if (!TryComp<CardsComponent>(split, out var newCardsComp))
        {
            return false;
        }

        // Animation must be before cards are moved
        var card = GetCardFromInx(cards.Comp.Cards, cardIndex);
        if (!card.HasValue)
        {
            if (!Exists(cards.Owner))
                return false;
            if (!TryComp<StackComponent>(split, out var splitStack))
                return false;
            var count = splitStack.Count;
            Stacks.SetCount((split.Value, splitStack), count - 1);
            count = stackComp.Count;
            Stacks.SetCount((cards.Owner, stackComp), count + 1);
            return false;
        }

        MoveCards(newCardsComp, cards.Comp, new List<CardData> { card.Value });
        // If this is true it is a new deck so copies over the properties
        // Otherwise it doesn't change the deck the card joins
        if (newCardsComp.Cards.Count == 1)
        {
            newCardsComp.Flipped = cards.Comp.Flipped;
            newCardsComp.Fanned = cards.Comp.Fanned;
            Hands.PickupOrDrop(user.Owner, split.Value);
        }

        Popup.PopupCursor(Loc.GetString("comp-stack-split"), user.Owner);

        UpdateVisualState(cards);
        UpdateVisualState((split.Value, newCardsComp));

        Dirty(cards.Owner, cards.Comp);
        Dirty(split.Value, newCardsComp);

        return true;
    }

    /// <summary>
    /// Finds the <see cref="CardData"/> in the given list whose card index matches the specified value.
    /// </summary>
    /// <param name="cards">The list of cards to search.</param>
    /// <param name="cardIndex">The card index to search for.</param>
    /// <returns>The matching <see cref="CardData"/>, or <c>null</c> if no card with that index exists.</returns>
    public CardData? GetCardFromInx(List<CardData> cards, int cardIndex)
    {
        var card = cards.Find(c => c.CardIndex == cardIndex);
        return card.CardId.Id == null ? null : card;
    }
}
