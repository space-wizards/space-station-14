using Content.Server.Hands.Components;
using Content.Server.Popups;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.PlayingCard;
using JetBrains.Annotations;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using System.Linq;
using Content.Shared.Examine;
using Robust.Server.GameObjects;
using Robust.Shared.Map;


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
    [Dependency] private readonly PlayingCardSystem _playingCardSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayingCardHandComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<PlayingCardHandComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<PlayingCardHandComponent, ExaminedEvent>(OnExamine);
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

        cardHandComponent.UserInterface?.Toggle(actor.PlayerSession);
    }

    private void OnExamine(EntityUid uid, PlayingCardHandComponent cardHandComponent, ExaminedEvent args)
    {
        args.PushText(Loc.GetString("playing-card-hand-component-examine-top-card", ("card", cardHandComponent.CardList.Last())));
        args.PushText(Loc.GetString("playing-card-hand-component-examine-count", ("count", cardHandComponent.CardList.Count)));
    }

    private void PickSingleCardMessage(EntityUid uid, PlayingCardHandComponent cardHandComponent, PickSingleCardMessage args)
    {
        if (args.Session.AttachedEntity is not {Valid: true} player)
            return;
        RemoveSingleCard(uid, args.Session.AttachedEntity, args.ID, cardHandComponent);
    }

    public void AddCards(EntityUid uid, EntityUid addedEntity, EntityUid user, PlayingCardHandComponent? cardHandComponent)
    {
        if (!Resolve(uid, ref cardHandComponent))
            return;

        if (TryComp<PlayingCardHandComponent>(addedEntity, out PlayingCardHandComponent? handComp))
        {
            if (handComp.CardDeckID != cardHandComponent.CardDeckID)
            {
                _popupSystem.PopupEntity(Loc.GetString("playing-card-hand-component-merge-card-id-fail"),
                    uid, Filter.Entities(user));
                return;
            }
            int addCount = handComp.CardList.Count;
            cardHandComponent.CardList.AddRange(handComp.CardList);
            EntityManager.QueueDeleteEntity(handComp.Owner);
            cardHandComponent.UserInterface?.SendMessage(new CardListMessage(cardHandComponent.CardList));
            UpdateAppearance(cardHandComponent);
            UpdateUiState(uid, cardHandComponent);
        }
        if (TryComp<PlayingCardComponent>(addedEntity, out PlayingCardComponent? cardComp))
        {
            if (cardComp.CardDeckID != cardHandComponent.CardDeckID)
            {
                _popupSystem.PopupEntity(Loc.GetString("playing-card-hand-component-merge-card-id-fail"),
                    uid, Filter.Entities(user));
                return;
            }
            cardHandComponent.CardList.Add(cardComp.CardName);
            EntityManager.QueueDeleteEntity(cardComp.Owner);
            cardHandComponent.UserInterface?.SendMessage(new CardListMessage(cardHandComponent.CardList));
            UpdateAppearance(cardHandComponent);
            UpdateUiState(uid, cardHandComponent);
        }
    }

    public void RemoveSingleCard(EntityUid uid, EntityUid? user, int cardIndex, PlayingCardHandComponent? cardHandComponent)
    {
        if (!Resolve(uid, ref cardHandComponent))
            return;

        if (user == null || cardHandComponent.CardList.Count <= 0)
            return;

        if (!TryComp<TransformComponent>(cardHandComponent.Owner, out var transformComp))
            return;

        if (TryComp<HandsComponent>(user, out var hands))
        {
            int freeHands = hands.GetFreeHands();
            if (freeHands == 0)
            {
                _popupSystem.PopupEntity(Loc.GetString("playing-card-deck-component-pickup-card-full-hand-fail"),
                    uid, Filter.Entities(user.Value));
                return;
            }

            string cardName = cardHandComponent.CardList[cardIndex];

            EntityUid? createdCard = _playingCardSystem.CreateCard(cardHandComponent.CardDeckID, cardName, cardHandComponent.CardPrototype, cardHandComponent.PlayingCardContentPrototypeID, transformComp.Coordinates, true);

            if (createdCard == null || !TryComp<SharedItemComponent>(createdCard, out var item))
                return;

                cardHandComponent.CardList.RemoveAt(cardIndex);

                hands.PutInHand(item);

            if (cardHandComponent.CardList.Count < 2)
            {
                string lastCardName = cardHandComponent.CardList[0];

                EntityUid? lastPlayingCardEnt = _playingCardSystem.CreateCard(cardHandComponent.CardDeckID, lastCardName, cardHandComponent.CardPrototype, cardHandComponent.PlayingCardContentPrototypeID, transformComp.Coordinates, true);

                if (lastPlayingCardEnt == null)
                    return;

                EntityManager.DeleteEntity(cardHandComponent.Owner);

                if (!TryComp<SharedItemComponent>(lastPlayingCardEnt, out var lastCardItem))
                    return;

                hands.PutInHand(lastCardItem);
            }
            else
            {
                UpdateUiState(uid, cardHandComponent);
                UpdateAppearance(cardHandComponent);
            }
        }
    }

    public EntityUid? CreateCardHand(string cardDeckID, List<string> cards, string cardHandPrototype, string playingCardPrototype, string playingCardContentPrototypeID, EntityCoordinates coords)
    {
        EntityUid playingCardHandEnt = EntityManager.SpawnEntity(cardHandPrototype, coords);

        if (!TryComp<PlayingCardHandComponent>(playingCardHandEnt, out PlayingCardHandComponent? playingCardHandComp))
        {
            EntityManager.DeleteEntity(playingCardHandEnt);
            return null;
        }

        playingCardHandComp.CardList = cards;
        playingCardHandComp.CardPrototype = playingCardPrototype;
        playingCardHandComp.CardDeckID = cardDeckID;
        playingCardHandComp.PlayingCardContentPrototypeID = playingCardContentPrototypeID;

        UpdateAppearance(playingCardHandComp);
        UpdateUiState(playingCardHandEnt, playingCardHandComp);
        return playingCardHandEnt;
    }

    private void UpdateAppearance(PlayingCardHandComponent cardHandComponent)
    {
        if (TryComp<AppearanceComponent>(cardHandComponent.Owner, out AppearanceComponent? appearance))
        {
            List<string> relevantCards = new(
                cardHandComponent.CardList.Skip(Math.Max(0, cardHandComponent.CardList.Count - 5)).Take(5)
            );

            appearance.SetData(PlayingCardHandVisuals.CardList, new CardListVisualState(relevantCards, cardHandComponent.PlayingCardContentPrototypeID));
        }
    }

    private void UpdateUiState(EntityUid uid, PlayingCardHandComponent? cardHandComponent)
    {
        if (!Resolve(uid, ref cardHandComponent))
            return;

        _userInterfaceSystem.TrySetUiState(uid, PlayingCardHandUiKey.Key, new PlayingCardHandBoundUserInterfaceState(cardHandComponent.CardList));
    }
}
