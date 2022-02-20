using System;
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

namespace Content.Server.PlayingCard
{
    [UsedImplicitly]
    public class PlayingCardDeckSystem : SharedPlayingCardDeckSystem
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
            // if deck of cards, add all, else add single card to list and destroy the entity
            AddCards(uid, args.Used, args.User,  cardDeckComponent);
        }

        private void OnInteractHand(EntityUid uid, PlayingCardDeckComponent cardDeckComponent, InteractHandEvent args)
        {
            // grab single card from top of deck
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
            // CHECK CARD IDS FIRST
            if (TryComp<PlayingCardHandComponent>(addedEntity, out PlayingCardHandComponent? handComp))
            {
                handComp.CardList.Reverse();
                cardDeckComponent.CardList.AddRange(handComp.CardList);
            }
            if (TryComp<PlayingCardComponent>(addedEntity, out PlayingCardComponent? cardComp))
            {
                // this needs to be card ID
                cardDeckComponent.CardList.Add(cardComp.CardName);
            }
        }

        public void PickupSingleCard(EntityUid uid, EntityUid user, PlayingCardDeckComponent cardDeckComponent)
        {

        }

        public void PickupMultipleCards(EntityUid uid, EntityUid user, PlayingCardDeckComponent cardDeckComponent)
        {

            // this creates cardhandcomponents
        }
    }
}
