using Content.Shared.Interaction;
using Content.Shared.PlayingCard;
using Content.Server.Popups;
using Content.Server.Hands.Components;
using Content.Shared.Item;
using Robust.Shared.Player;
using Content.Shared.Examine;
using Robust.Shared.Map;

namespace Content.Server.PlayingCard.EntitySystems;

public sealed class PlayingCardSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly PlayingCardHandSystem _playingCardHandSystem = default!;

    public override void Initialize()
    {
        // ON INIT, set card sprite
        SubscribeLocalEvent<PlayingCardComponent, UseInHandEvent>(OnUseInhand);
        SubscribeLocalEvent<PlayingCardComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<PlayingCardComponent, ExaminedEvent>(OnExamined);
    }

    private void OnUseInhand(EntityUid uid, PlayingCardComponent cardComponent, UseInHandEvent args)
    {
        if (args.Handled) return;
        FlipCard(cardComponent, args.User);
        args.Handled = true;
    }

    private void OnInteractUsing(EntityUid uid, PlayingCardComponent cardComponent, InteractUsingEvent args)
    {
        CombineCards(uid, args.Used, args.User, cardComponent);
    }

    private void OnExamined(EntityUid uid, PlayingCardComponent cardComponent, ExaminedEvent args)
    {
        if (cardComponent.FacingUp)
            args.PushText(cardComponent.CardName);
        // else
            // args.PushText(Loc.GetString("playing-card-deck-component-examine-multiple", ("count", cardDeckComponent.CardList.Count)));
    }

    public void CombineCards(EntityUid uid, EntityUid itemUsed, EntityUid user, PlayingCardComponent cardComponent)
    {
        if (TryComp<PlayingCardComponent>(itemUsed, out PlayingCardComponent? incomingCardComp))
            {

                if (incomingCardComp.StackTypeId != cardComponent.StackTypeId)
                {
                    _popupSystem.PopupEntity(Loc.GetString("playing-card-hand-component-merge-card-id-fail"),
                        uid, Filter.Entities(uid));
                    return;
                }

                if (!TryComp<HandsComponent>(user, out var hands))
                    return;

                if (!TryComp<TransformComponent>(cardComponent.Owner, out var transformComp))
                return;


                List<string> cardsToAdd = new();
                cardsToAdd.Add(incomingCardComp.CardName);
                cardsToAdd.Add(cardComponent.CardName);

                EntityUid? cardHand =  _playingCardHandSystem.CreateCardHand(cardsToAdd, cardComponent.CardHandPrototype, transformComp.Coordinates);

                if (cardHand == null || !TryComp<SharedItemComponent>(cardHand, out var cardHandEnt))
                    return;

                EntityManager.QueueDeleteEntity(itemUsed);
                EntityManager.QueueDeleteEntity(cardComponent.Owner);

                hands.PutInHand(cardHandEnt);
            }
    }

    private void FlipCard(PlayingCardComponent component, EntityUid user)
    {
        if (component.FacingUp)
        {
            component.FacingUp = false;
            if (TryComp<AppearanceComponent>(component.Owner, out AppearanceComponent? appearance))
            {
                appearance.SetData(PlayingCardVisuals.FacingUp, false);
            }
            // use improper name
            // use improper description
        }
        else
        {
            component.FacingUp = true;
            if (TryComp<AppearanceComponent>(component.Owner, out AppearanceComponent? appearance))
            {
                appearance.SetData(PlayingCardVisuals.FacingUp, true);
            }
            // assign proper name
            // assign proper description
        }
    }

    public EntityUid? CreateCard(string cardName, string cardPrototype, EntityCoordinates coords)
    {

        EntityUid playingCardEnt = EntityManager.SpawnEntity(cardPrototype, coords);
        if (!TryComp<PlayingCardComponent>(playingCardEnt, out PlayingCardComponent? playingCardComp))
        {
            EntityManager.DeleteEntity(playingCardEnt);
            return null;
        }
        playingCardComp.CardName = cardName;
        if (TryComp<AppearanceComponent>(playingCardEnt, out AppearanceComponent? appearance))
        {
            appearance.SetData(PlayingCardVisuals.CardSprite, cardName);
        }
        return playingCardEnt;
    }
}
