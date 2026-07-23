using System.Linq;
using Content.Shared.Examine;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Random.Helpers;
using Content.Shared.Verbs;
using Robust.Shared.Random;

namespace Content.Shared.Cards;

public abstract partial class SharedCardSystem
{
    // When 'E' pressed in the world
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

    [SubscribeLocalEvent]
    private void OnPickupEvent(Entity<CardsComponent> ent, ref HandSelectedEvent args)
    {
        UpdateVisualState(ent);
    }

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

        args.Verbs.Add(
            new AlternativeVerb
            {
                Text = Loc.GetString("comp-cards-flip"),
                Act = () => TryFlipCards(ent),
                Priority = -98,
            }
        );

        args.Verbs.Add(
            new AlternativeVerb
            {
                Text = Loc.GetString("comp-cards-shuffle"),
                Act = () => TryShuffleCards(ent),
                Priority = -99,
            }
        );

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
                        Act = () => TryTakeCard(ent, user, card.CardInx, out _),
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
                    Act = () => TryTakeCard(ent, user, ent.Comp.Cards[randomIndex].CardInx, out _),
                    Priority = -200,
                }
            );
        }
    }
}
