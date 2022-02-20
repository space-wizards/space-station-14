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
    public class PlayingCardSystem : SharedPlayingCardSystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;

        public static readonly int PullCardLimit = 10;

        public override void Initialize()
        {
            base.Initialize();

            // SubscribeLocalEvent<PlayingCardComponent, InteractUsingEvent>(OnStackInteractUsing);
            // // SubscribeLocalEvent<PlayingCardComponent, GetVerbsEvent<AlternativeVerb>>(OnStackAlternativeInteract);
            // // SubscribeLocalEvent<PlayingCardComponent, UseInHandEvent>(OnUseInHand);
            // // SubscribeLocalEvent<PlayingCardComponent, ExaminedEvent(examine);

        }


        private void OnUiseInHand(EntityUid uid, PlayingCardHandComponent cardComponent, UseInHandEvent args)
        {
            // if (single)
            // flip

            // if deck
            // shuffle

            // if not deck and more than 1 card
            // view interface
        }

        // Should inspect upright cards if they they're less than 10
    }
}
