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

namespace Content.Server.PlayingCard
{
    [UsedImplicitly]
    public class PlayingCardDeckSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        public static readonly int PickupMultipleCardLimit = 10;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PlayingCardDeckComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<PlayingCardDeckComponent, UseInHandEvent>(OnUseInHand);
            SubscribeLocalEvent<PlayingCardDeckComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<PlayingCardDeckComponent, InteractHandEvent>(OnInteractHand);
            SubscribeLocalEvent<PlayingCardDeckComponent, GetVerbsEvent<AlternativeVerb>>(AddAltVerb);

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
            // CREATE COOLDOWN
            Shuffle(uid, args.User, cardDeckComponent);
        }

        private void OnInteractUsing(EntityUid uid, PlayingCardDeckComponent cardDeckComponent, InteractUsingEvent args)
        {
            AddCards(uid, args.Used, args.User,  cardDeckComponent);
        }

        private void OnInteractHand(EntityUid uid, PlayingCardDeckComponent cardDeckComponent, InteractHandEvent args)
        {
            PickupSingleCard(uid, args.User, cardDeckComponent);
        }

        private void AddAltVerb(EntityUid uid, PlayingCardDeckComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            //  add verbs for picking up deck and taking cards out
          if (!args.CanInteract)
                 return;

            AlternativeVerb verb = new()
            {
                Act = () =>
                {
                    PickupMultipleCards(uid, args.User, component);
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
            while (count > 1) {
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
                if (handComp.StackTypeId != cardDeckComponent.StackTypeId)
                {
                    _popupSystem.PopupEntity(Loc.GetString("playing-card-deck-component-merge-card-id-fail"),
                        uid, Filter.Entities(uid));
                    return;
                }
                // handComp.CardList.Reverse();
                int addCount = handComp.CardList.Count;
                cardDeckComponent.CardList.AddRange(handComp.CardList);
                EntityManager.QueueDeleteEntity(handComp.Owner);
                _popupSystem.PopupEntity(Loc.GetString("playing-card-deck-component-add-multiple", ("user", user), ("count", addCount)),
                    uid, Filter.Pvs(uid));
            }
            if (TryComp<PlayingCardComponent>(addedEntity, out PlayingCardComponent? cardComp))
            {
                if (cardComp.StackTypeId != cardDeckComponent.StackTypeId)
                {
                    _popupSystem.PopupEntity(Loc.GetString("playing-card-deck-component-merge-card-id-fail"),
                        uid, Filter.Entities(uid));
                    return;
                }
                cardDeckComponent.CardList.Add(cardComp.CardName);
                EntityManager.QueueDeleteEntity(cardComp.Owner);
                _popupSystem.PopupEntity(Loc.GetString("playing-card-deck-component-add-single", ("user", user)),
                    uid, Filter.Pvs(uid));
            }
        }

        public void PickupSingleCard(EntityUid uid, EntityUid user, PlayingCardDeckComponent cardDeckComponent)
        {
            if (cardDeckComponent.CardList.Count <= 0)
            {
                return;
            }

            if (!TryComp<TransformComponent>(cardDeckComponent.Owner, out var transformComp))
                return;

            if (!TryComp<HandsComponent>(user, out var hands))
            {
                _popupSystem.PopupEntity(Loc.GetString("playing-card-deck-component-pickup-card-full-hand-fail"),
                uid, Filter.Entities(uid));
                return;
            }

            string topCard = cardDeckComponent.CardList.First();
            EntityUid playingCard = Spawn(cardDeckComponent.CardPrototype, transformComp.Coordinates);

            if (!TryComp<SharedItemComponent>(playingCard, out var item))
                return;

            if (!TryComp<PlayingCardComponent>(playingCard, out PlayingCardComponent? playingCardComp))
            {
                EntityManager.DeleteEntity(playingCard);
                return;
            }

            playingCardComp.CardName = topCard;
            // ALSO ASSIGN DESCRIPTION
            hands.PutInHand(item);
            cardDeckComponent.CardList.RemoveAt(0);
            _popupSystem.PopupEntity(Loc.GetString("playing-card-deck-component-pick-up-single", ("user", user)),
                uid, Filter.Pvs(uid));

        }

        public void PickupMultipleCards(EntityUid uid, EntityUid user, PlayingCardDeckComponent cardDeckComponent)
        {
            // THIS NEEDS TO CALL A MINI UI INPUT TO GRAB INPUT
            int count = 5;
            int givenCardAmount = count;

            if (cardDeckComponent.CardList.Count <= 0)
            {
                return;
            }

            if (count < 1)
                return;

            if (count > cardDeckComponent.CardList.Count)
                givenCardAmount = cardDeckComponent.CardList.Count;

            if (!TryComp<TransformComponent>(cardDeckComponent.Owner, out var transformComp))
                return;


            if (TryComp<HandsComponent>(user, out var hands))
            {
                var topCards = cardDeckComponent.CardList.Take(givenCardAmount);
                EntityUid playingCards = Spawn(cardDeckComponent.CardHandPrototype, transformComp.Coordinates);
                if (TryComp<SharedItemComponent>(playingCards, out var item))
                {
                    hands.PutInHand(item);
                    _popupSystem.PopupEntity(Loc.GetString("playing-card-deck-component-pick-up-multiple", ("user", user), ("count", givenCardAmount)),
                        uid, Filter.Pvs(uid));
                    cardDeckComponent.CardList.RemoveRange(0, givenCardAmount);
                }
            }
            _popupSystem.PopupEntity(Loc.GetString("playing-card-deck-component-pickup-card-full-hand-fail"),
                uid, Filter.Entities(uid));
        }
    }
}
