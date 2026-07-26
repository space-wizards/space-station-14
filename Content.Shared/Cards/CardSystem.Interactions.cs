using System.Linq;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Random.Helpers;
using Content.Shared.Verbs;

namespace Content.Shared.Cards;

public abstract partial class SharedCardSystem
{
    // When 'E' pressed in the world
    // Flips the deck
    [SubscribeLocalEvent]
    private void OnCardsActivate(Entity<CardsComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        args.Handled = true;
        TryFlipCards(ent);
    }

    // When 'Z' pressed in hands
    // Will flip then fan then flip and fan
    [SubscribeLocalEvent]
    private void OnCardsUse(Entity<CardsComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (ent.Comp.Fanned)
        {
            TryFanCards(ent);
            TryFlipCards(ent);
        }
        else if (ent.Comp.Flipped && ent.Comp.Cards.Count != 1)
        {
            TryFanCards(ent);
        }
        else
        {
            TryFlipCards(ent);
        }
    }

    // Used for strip menu visuals. Need to updated whenever moved into or out of inventory.
    [SubscribeLocalEvent]
    private void OnPickupEvent(Entity<CardsComponent> ent, ref HandSelectedEvent args)
    {
        UpdateVisualState(ent);
    }

    // Used for strip menu visuals. Need to updated whenever moved into or out of inventory.
    [SubscribeLocalEvent]
    protected virtual void OnCardsDropped(Entity<CardsComponent> ent, ref DroppedEvent args)
    {
        UpdateVisualState(ent);
    }

    [SubscribeLocalEvent]
    private void OnCardsAlternativeInteract(Entity<CardsComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract || args.Hands == null)
            return;

        var user = args.User;

        // Verbs here have low priority so they are always below the stack split verbs

        // Flip verb
        args.Verbs.Add(
            new AlternativeVerb
            {
                Text = Loc.GetString("comp-cards-flip"),
                Act = () => TryFlipCards(ent),
                Priority = -98,
            }
        );

        // Shuffle verb
        args.Verbs.Add(
            new AlternativeVerb
            {
                Text = Loc.GetString("comp-cards-shuffle"),
                Act = () => TryShuffleCards(ent),
                Priority = -99,
            }
        );

        // Fan verb
        // Can only fan when not inside a container
        if (
            !Container.TryGetContainingContainer(ent.Owner, out var container)
            || Hands.EnumerateHands(container.Owner).Contains(container.ID)
        )
            args.Verbs.Add(
                new AlternativeVerb
                {
                    Text = Loc.GetString("comp-cards-fan"),
                    Act = () => TryFanCards(ent),
                    Priority = -100,
                }
            );
        var priority = -200;

        // Take card verbs
        // Can only take card when fanned
        // If face down will take a random card
        if (!ent.Comp.Fanned)
            return;
        if (ent.Comp.Flipped)
        {
            for (var i = 0; i < ent.Comp.Cards.Count; i++)
            {
                var card = ent.Comp.Cards[i];
                args.Verbs.Add(
                    new AlternativeVerb
                    {
                        Text = Loc.GetString(card.CardId.ToString().Replace('_', '-')),
                        Act = () => TryTakeCard(ent, user, card.CardIndex, out _),
                        Category = VerbCategory.TakeCard,
                        Priority = priority--,
                    }
                );
            }
        }
        else
        {
            var randomIndex = SharedRandomExtensions
                .PredictedRandom(Timing, GetNetEntity(ent))
                .Next(ent.Comp.Cards.Count);
            args.Verbs.Add(
                new AlternativeVerb
                {
                    Text = Loc.GetString("comp-cards-random-card"),
                    Act = () => TryTakeCard(ent, user, ent.Comp.Cards[randomIndex].CardIndex, out _),
                    Priority = -200,
                }
            );
        }
    }
}
