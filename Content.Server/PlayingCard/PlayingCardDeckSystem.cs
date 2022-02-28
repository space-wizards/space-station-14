using Content.Server.Hands.Components;
using Content.Server.Popups;
using Content.Shared.Interaction;
using Content.Shared.PlayingCard;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Content.Shared.Examine;
using Robust.Shared.Audio;
using Content.Shared.Item;
using System.Linq;
using Robust.Server.GameObjects;
using Robust.Shared.Map;

namespace Content.Server.PlayingCard.EntitySystems;
[UsedImplicitly]
public class PlayingCardDeckSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly PlayingCardSystem _playingCardSystem = default!;
    [Dependency] private readonly PlayingCardHandSystem _playingCardHandSystem = default!;

    public static readonly int PickupMultipleCardLimit = 10;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayingCardDeckComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<PlayingCardDeckComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<PlayingCardDeckComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<PlayingCardDeckComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<PlayingCardDeckComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<PlayingCardDeckComponent, GetVerbsEvent<AlternativeVerb>>(AddAltVerb);
        SubscribeLocalEvent<PlayingCardDeckComponent, PickupCountMessage>(PickupMultipleCards);
    }

    private void OnComponentInit(EntityUid uid, PlayingCardDeckComponent cardDeckComponent, ComponentInit args)
    {
        if (cardDeckComponent.CardDeckID == string.Empty)
        {
            cardDeckComponent.CardDeckID = cardDeckComponent.Owner.ToString();
        }
    }

    private void OnExamined(EntityUid uid, PlayingCardDeckComponent cardDeckComponent, ExaminedEvent args)
    {
        if (cardDeckComponent.CardList.Count == 1)
            args.PushText(Loc.GetString("playing-card-deck-component-examine-single", ("count", cardDeckComponent.CardList.Count)));
        else
            args.PushText(Loc.GetString("playing-card-deck-component-examine-multiple", ("count", cardDeckComponent.CardList.Count)));
    }


    private void OnUseInHand(EntityUid uid, PlayingCardDeckComponent cardDeckComponent, UseInHandEvent args)
    {
        if (args.Handled) return;

        args.Handled = true;
        Shuffle(uid, args.User, cardDeckComponent);
    }

    private void OnInteractUsing(EntityUid uid, PlayingCardDeckComponent cardDeckComponent, InteractUsingEvent args)
    {
        AddCards(uid, args.Used, args.User, cardDeckComponent);
    }

    private void OnInteractHand(EntityUid uid, PlayingCardDeckComponent cardDeckComponent, InteractHandEvent args)
    {
        PickupSingleCard(uid, args.User, cardDeckComponent);
    }

    private void AddAltVerb(EntityUid uid, PlayingCardDeckComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract)
            return;

        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                OpenCardPickUI(uid, args.User, component);
            },
            Text = Loc.GetString("playing-card-deck-component-pickup-multiple-verb"),
            Priority = 2
        };
        args.Verbs.Add(verb);
    }

    public void Shuffle(EntityUid uid, EntityUid user, PlayingCardDeckComponent cardDeckComponent)
    {
        Random rng = new Random();

        List<string> cards = cardDeckComponent.CardList;
        int count = cards.Count;
        while (count > 1)
        {
            count--;
            int next = rng.Next(count + 1);
            string value = cards[next];
            cards[next] = cards[count];
            cards[count] = value;
        }
        _popupSystem.PopupEntity(Loc.GetString("playing-card-deck-component-shuffle", ("user", user)),
            uid, Filter.Pvs(uid));
        SoundSystem.Play(Filter.Pvs(uid), cardDeckComponent.ShuffleSound.GetSound(), uid);
    }

    public void AddCards(EntityUid uid, EntityUid addedEntity, EntityUid user, PlayingCardDeckComponent cardDeckComponent)
    {
        if (TryComp<PlayingCardHandComponent>(addedEntity, out PlayingCardHandComponent? handComp))
        {
            if (handComp.CardDeckID != cardDeckComponent.CardDeckID)
            {
                _popupSystem.PopupEntity(Loc.GetString("playing-card-deck-component-merge-card-id-fail"),
                    uid, Filter.Entities(user));
                return;
            }

            int addCount = handComp.CardList.Count;
            cardDeckComponent.CardList.AddRange(handComp.CardList);
            EntityManager.QueueDeleteEntity(handComp.Owner);
            _popupSystem.PopupEntity(Loc.GetString("playing-card-deck-component-add-multiple", ("user", user), ("count", addCount)),
                uid, Filter.Pvs(uid));
        }
        if (TryComp<PlayingCardComponent>(addedEntity, out PlayingCardComponent? cardComp))
        {
            if (cardComp.CardDeckID != cardDeckComponent.CardDeckID)
            {
                _popupSystem.PopupEntity(Loc.GetString("playing-card-deck-component-merge-card-id-fail"),
                    uid, Filter.Entities(user));
                return;
            }
            cardDeckComponent.CardList.Add(cardComp.CardName);
            EntityManager.QueueDeleteEntity(cardComp.Owner);
            _popupSystem.PopupEntity(Loc.GetString("playing-card-deck-component-add-single", ("user", user)),
                uid, Filter.Pvs(uid));
        }
    }

    public void PickupSingleCard(EntityUid uid, EntityUid? user, PlayingCardDeckComponent cardDeckComponent)
    {
        if (user == null)
            return;

        if (cardDeckComponent.CardList.Count <= 0)
        {
            _popupSystem.PopupEntity(Loc.GetString("playing-card-deck-component-pickup-card-none-left"),
                uid, Filter.Entities(user.Value));
            return;
        }

        if (!TryComp<TransformComponent>(cardDeckComponent.Owner, out var transformComp))
            return;

        if (!TryComp<HandsComponent>(user, out var hands))
        {
            _popupSystem.PopupEntity(Loc.GetString("playing-card-deck-component-pickup-card-full-hand-fail"),
            uid, Filter.Entities(user.Value));
            return;
        }

        string topCard = cardDeckComponent.CardList.First();

        EntityUid? playingCard = _playingCardSystem.CreateCard(cardDeckComponent.CardDeckID,
            topCard,
            cardDeckComponent.CardPrototype,
            cardDeckComponent.NoUniqueCardLayers,
            transformComp.Coordinates);

        if (!TryComp<SharedItemComponent>(playingCard, out var item))
            return;

        hands.PutInHand(item);
        cardDeckComponent.CardList.RemoveAt(0);
        _popupSystem.PopupEntity(Loc.GetString("playing-card-deck-component-pick-up-single", ("user", user)),
            uid, Filter.Pvs(uid));
    }

    public void OpenCardPickUI(EntityUid uid, EntityUid user, PlayingCardDeckComponent cardDeckComponent)
    {
        if (!TryComp<ActorComponent>(user, out var actor))
            return;

        cardDeckComponent.UserInterface?.Toggle(actor.PlayerSession);
    }

    public void PickupMultipleCards(EntityUid uid, PlayingCardDeckComponent cardDeckComponent, PickupCountMessage args)
    {
        int count = args.Count;

        if (count < 0)
            return;

        if (cardDeckComponent.CardList.Count <= 0 && args.Session.AttachedEntity != null)
        {
            _popupSystem.PopupEntity(Loc.GetString("playing-card-deck-component-pickup-card-none-left"),
                uid, Filter.Entities(args.Session.AttachedEntity.Value));
            return;
        }

        if (count == 1)
        {
            PickupSingleCard(uid, args.Session.AttachedEntity, cardDeckComponent);
            return;
        }

        if (count > cardDeckComponent.CardList.Count)
            count = cardDeckComponent.CardList.Count;

        if (!TryComp<TransformComponent>(cardDeckComponent.Owner, out var transformComp))
            return;

        if (TryComp<HandsComponent>(args.Session.AttachedEntity, out var hands))
        {
            var topCards = cardDeckComponent.CardList.Take(count).ToList();

            EntityUid? playingCards = _playingCardHandSystem.CreateCardHand(cardDeckComponent.CardDeckID,
                topCards,
                cardDeckComponent.CardHandPrototype,
                cardDeckComponent.CardPrototype,
                cardDeckComponent.NoUniqueCardLayers,
                transformComp.Coordinates);

            if (playingCards != null && TryComp<SharedItemComponent>(playingCards, out var item))
            {
                hands.PutInHand(item);
                if (TryComp<MetaDataComponent>(args.Session.AttachedEntity, out MetaDataComponent? metaData))
                {
                    _popupSystem.PopupEntity(Loc.GetString("playing-card-deck-component-pick-up-multiple", ("user", metaData.EntityName), ("count", count)),
                        uid, Filter.Pvs(uid));
                }
                cardDeckComponent.CardList.RemoveRange(0, count);
            }
            return;
        }
        _popupSystem.PopupEntity(Loc.GetString("playing-card-deck-component-pickup-card-full-hand-fail"),
            uid, Filter.Entities(uid));
    }
}
