using System;
using Content.Server.Hands.Components;
using Content.Server.Popups;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.PlayingCard;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Content.Server.PlayingCard;
using System.Linq;
using Content.Shared.Examine;


namespace Content.Server.PlayingCard
{
    /// <summary>
    ///     Entity system that handles everything relating to stacks.
    ///     This is a good example for learning how to code in an ECS manner.
    /// </summary>
    [UsedImplicitly]
    public class PlayingCardSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;

        public static readonly int PullCardLimit = 10;

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
            if (TryComp<PlayingCardComponent>(args.Used, out PlayingCardComponent? cardComp))
            {
                cardHandComponent.CardList.Add(cardComp.CardName);
                EntityManager.QueueDeleteEntity(cardComp.Owner);
                // ADDED CARD VISUAL
            }
        }

        private void OnUseInHand(EntityUid uid, PlayingCardHandComponent cardHandComponent, UseInHandEvent args)
        {
            // view interface
        }

        private void OnExamine(EntityUid uid, PlayingCardHandComponent cardHandComponent, ExaminedEvent args)
        {
            // List last 5 cards

            args.PushText(Loc.GetString("playing-card-hand-component-examine", ("count", cardHandComponent.CardList.Last())));
        }

        private void OnCardListSyncRequest(EntityUid uid, PlayingCardHandComponent cardHandComponent, CardListSyncRequestMessage args)
        {

        }

        private void PickSingleCardMessage(EntityUid uid, PlayingCardHandComponent cardHandComponent, PickSingleCardMessage args)
        {

        }

        public void RemoveSingleCard(EntityUid uid, EntityUid user, int cardIndex, PlayingCardHandComponent cardHandComponent, PickSingleCardMessage args)
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
    }
}
