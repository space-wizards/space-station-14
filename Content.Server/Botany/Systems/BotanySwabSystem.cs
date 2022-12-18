using System.Threading;
using Content.Server.Botany.Components;
using Content.Server.DoAfter;
using Content.Server.Hands.Components;
using Content.Server.Nutrition.EntitySystems;
using Content.Server.Popups;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Tools.Components;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Botany.Systems
{
    public sealed class BotanySwabSystem : EntitySystem
    {
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly MutationSystem _mutationSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<BotanySwabComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<BotanySwabComponent, ExaminedEvent>(OnExamined);
            // Private Events
            SubscribeLocalEvent<TargetSwabSuccessfulEvent>(OnTargetSwabSuccessful);
            SubscribeLocalEvent<SwabCancelledEvent>(OnSwabCancelled);
        }

        /// <summary>
        /// Handles swabbing a plant.
        /// </summary>
        private void OnAfterInteract(EntityUid uid, BotanySwabComponent swab, AfterInteractEvent args)
        {
            if (swab.CancelToken != null)
            {
                swab.CancelToken.Cancel();
                swab.CancelToken = null;
                return;
            }

            if (args.Target == null || !args.CanReach)
                return;

            if (!TryComp<PlantHolderComponent>(args.Target, out var plant))
                return;

            swab.CancelToken = new CancellationTokenSource();
            _doAfterSystem.DoAfter(new DoAfterEventArgs(args.User, swab.SwabDelay, swab.CancelToken.Token, target: args.Target)
            {
                BroadcastFinishedEvent = new TargetSwabSuccessfulEvent(args.User, args.Target, swab, plant),
                BroadcastCancelledEvent = new SwabCancelledEvent(swab),
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnStun = true,
                NeedHand = true
            });
        }

        /// <summary>
        /// This handles swab examination text
        /// so you can tell if they are used or not.
        /// </summary>
        private void OnExamined(EntityUid uid, BotanySwabComponent swab, ExaminedEvent args)
        {
            if (args.IsInDetailsRange)
            {
                if (swab.SeedData != null)
                    args.PushMarkup(Loc.GetString("swab-used"));
                else
                    args.PushMarkup(Loc.GetString("swab-unused"));
            }
        }

        /// <summary>
        /// Save seed data or cross-pollenate.
        /// </summary>
        private void OnTargetSwabSuccessful(TargetSwabSuccessfulEvent args)
        {
            if (args.Target == null)
                return;

            if (args.Swab.SeedData == null)
            {
                // Pick up pollen
                args.Swab.SeedData = args.Plant.Seed;
                _popupSystem.PopupEntity(Loc.GetString("botany-swab-from"), args.Target.Value, args.User);
            }
            else
            {
                var old = args.Plant.Seed; // Save old plant pollen
                if (old == null)
                    return;
                args.Plant.Seed = _mutationSystem.Cross(args.Swab.SeedData, old); // Cross-pollenate
                args.Swab.SeedData = old; // Transfer old plant pollen to swab
                _popupSystem.PopupEntity(Loc.GetString("botany-swab-to"), args.Target.Value, args.User);
            }

            if (args.Swab.CancelToken != null)
            {
                args.Swab.CancelToken.Cancel();
                args.Swab.CancelToken = null;
            }
        }

        private static void OnSwabCancelled(SwabCancelledEvent args)
        {
            args.Swab.CancelToken = null;
        }

        private sealed class SwabCancelledEvent : EntityEventArgs
        {
            public readonly BotanySwabComponent Swab;
            public SwabCancelledEvent(BotanySwabComponent swab)
            {
                Swab = swab;
            }
        }

        private sealed class TargetSwabSuccessfulEvent : EntityEventArgs
        {
            public EntityUid User { get; }
            public EntityUid? Target { get; }
            public BotanySwabComponent Swab { get; }

            public PlantHolderComponent Plant { get; }

            public TargetSwabSuccessfulEvent(EntityUid user, EntityUid? target, BotanySwabComponent swab, PlantHolderComponent plant)
            {
                User = user;
                Target = target;
                Swab = swab;
                Plant = plant;
            }
        }
    }
}

