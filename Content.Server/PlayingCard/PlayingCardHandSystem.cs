using System;
using Content.Server.Hands.Components;
using Content.Server.Popups;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.PlayingCard;
using Content.Shared.Verbs;
using JetBrains.Annotations;

using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Content.Server.PlayingCard;
using System.Linq;
using Content.Shared.Examine;
using Robust.Server.GameObjects;

namespace Content.Server.PlayingCard.EntitySystems;
/// <summary>
///     Entity system that handles everything relating to stacks.
///     This is a good example for learning how to code in an ECS manner.
/// </summary>
[UsedImplicitly]
public class PlayingCardHandSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayingCardHandComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<PlayingCardHandComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<PlayingCardHandComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<PlayingCardHandComponent, CardListSyncRequestMessage>(OnCardListSyncRequest);
        SubscribeLocalEvent<PlayingCardHandComponent, PickSingleCardMessage>(PickSingleCardMessage);
    }

    private void OnInteractUsing(EntityUid uid, PlayingCardHandComponent cardHandComponent, InteractUsingEvent args)
    {
        AddCards(uid, args.Used, args.User, cardHandComponent);
    }

    private void OnUseInHand(EntityUid uid, PlayingCardHandComponent cardHandComponent, UseInHandEvent args)
    {
        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        Logger.Debug("attempt to create");
        cardHandComponent.UserInterface?.Toggle(actor.PlayerSession);
    }

    private void OnExamine(EntityUid uid, PlayingCardHandComponent cardHandComponent, ExaminedEvent args)
    {
        // List last 5 cards

        args.PushText(Loc.GetString("playing-card-hand-component-examine", ("cards", cardHandComponent.CardList.Last())));
    }

    private void OnCardListSyncRequest(EntityUid uid, PlayingCardHandComponent cardHandComponent, CardListSyncRequestMessage args)
    {
        cardHandComponent.UserInterface?.SendMessage(new CardListMessage(cardHandComponent.CardList));
    }

    private void PickSingleCardMessage(EntityUid uid, PlayingCardHandComponent cardHandComponent, PickSingleCardMessage args)
    {
        RemoveSingleCard(uid, args.Entity, args.ID, cardHandComponent);
    }

    public void AddCards(EntityUid uid, EntityUid addedEntity, EntityUid user, PlayingCardHandComponent? cardHandComponent)
    {
        if (!Resolve(uid, ref cardHandComponent))
            return;

        if (TryComp<PlayingCardHandComponent>(addedEntity, out PlayingCardHandComponent? handComp))
        {
            if (handComp.StackTypeId != cardHandComponent.StackTypeId)
            {
                _popupSystem.PopupEntity(Loc.GetString("playing-card-hand-component-merge-card-id-fail"),
                    uid, Filter.Entities(uid));
                return;
            }
            // handComp.CardList.Reverse();
            int addCount = handComp.CardList.Count;
            cardHandComponent.CardList.AddRange(handComp.CardList);
            EntityManager.QueueDeleteEntity(handComp.Owner);
            cardHandComponent.UserInterface?.SendMessage(new CardListMessage(cardHandComponent.CardList));

        }
        if (TryComp<PlayingCardComponent>(addedEntity, out PlayingCardComponent? cardComp))
        {
            if (cardComp.StackTypeId != cardHandComponent.StackTypeId)
            {
                _popupSystem.PopupEntity(Loc.GetString("playing-card-hand-component-merge-card-id-fail"),
                    uid, Filter.Entities(uid));
                return;
            }
            cardHandComponent.CardList.Add(cardComp.CardName);
            EntityManager.QueueDeleteEntity(cardComp.Owner);
            cardHandComponent.UserInterface?.SendMessage(new CardListMessage(cardHandComponent.CardList));
        }
    }

    public void RemoveSingleCard(EntityUid uid, EntityUid user, int cardIndex, PlayingCardHandComponent cardHandComponent)
    {
        if (cardHandComponent.CardList.Count <= 0)
        {
            return;
        }

        if (!TryComp<TransformComponent>(cardHandComponent.Owner, out var transformComp))
            return;

        if (TryComp<HandsComponent>(user, out var hands))
        {
            EntityUid playingCard = Spawn(cardHandComponent.CardPrototype, transformComp.Coordinates);
            // GRAB NAME FROM LIST
            string name = cardHandComponent.CardList[cardIndex];
            cardHandComponent.CardList.RemoveAt(cardIndex);
            if (TryComp<SharedItemComponent>(playingCard, out var item))
            {
                hands.PutInHand(item);
                if (cardHandComponent.CardList.Count < 2)
                {
                    string lastCardName = cardHandComponent.CardList[cardIndex];
                    cardHandComponent.CardList.RemoveAt(cardIndex);
                    EntityUid lastPlayingCard = Spawn(cardHandComponent.CardPrototype, transformComp.Coordinates);
                    EntityManager.QueueDeleteEntity(cardHandComponent.Owner);
                    if (TryComp<SharedItemComponent>(lastPlayingCard, out var lastCardItem))
                    {
                        hands.PutInHand(lastCardItem);
                    }
                }
            }
        }
        _popupSystem.PopupEntity(Loc.GetString("playing-card-deck-component-pickup-card-full-hand-fail"),
            uid, Filter.Entities(uid));
    }

    private void UpdateUiState(EntityUid uid, PlayingCardHandComponent? cardHandComponent)
    {
        if (!Resolve(uid, ref cardHandComponent))
            return;

        _userInterfaceSystem.TrySetUiState(uid, PlayingCardHandUiKey.Key, new PlayingCardHandBoundUserInterfaceState(cardHandComponent.CardList));
    }
}
