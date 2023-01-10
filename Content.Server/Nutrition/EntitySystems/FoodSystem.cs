using System.Threading;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.DoAfter;
using Content.Server.Hands.Components;
using Content.Server.MobState;
using Content.Server.Nutrition.Components;
using Content.Server.Popups;
using Content.Shared.Administration.Logs;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.MobState.Components;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Nutrition.EntitySystems
{
    /// <summary>
    /// Handles feeding attempts both on yourself and on the target.
    /// </summary>
    public sealed class FoodSystem : EntitySystem
    {
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly FlavorProfileSystem _flavorProfileSystem = default!;
        [Dependency] private readonly BodySystem _bodySystem = default!;
        [Dependency] private readonly StomachSystem _stomachSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
        [Dependency] private readonly UtensilSystem _utensilSystem = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
        [Dependency] private readonly InventorySystem _inventorySystem = default!;
        [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<FoodComponent, UseInHandEvent>(OnUseFoodInHand);
            SubscribeLocalEvent<FoodComponent, AfterInteractEvent>(OnFeedFood);
            SubscribeLocalEvent<FoodComponent, GetVerbsEvent<AlternativeVerb>>(AddEatVerb);
            SubscribeLocalEvent<BodyComponent, FeedEvent>(OnFeed);
            SubscribeLocalEvent<ForceFeedCancelledEvent>(OnFeedCancelled);
            SubscribeLocalEvent<InventoryComponent, IngestionAttemptEvent>(OnInventoryIngestAttempt);
        }

        /// <summary>
        /// Eat item
        /// </summary>
        private void OnUseFoodInHand(EntityUid uid, FoodComponent foodComponent, UseInHandEvent ev)
        {
            if (ev.Handled)
                return;

            ev.Handled = TryFeed(ev.User, ev.User, foodComponent);
        }

        /// <summary>
        /// Feed someone else
        /// </summary>
        private void OnFeedFood(EntityUid uid, FoodComponent foodComponent, AfterInteractEvent args)
        {
            if (args.Handled || args.Target == null || !args.CanReach)
                return;

            args.Handled = TryFeed(args.User, args.Target.Value, foodComponent);
        }

        public bool TryFeed(EntityUid user, EntityUid target, FoodComponent food)
        {
            // if currently being used to feed, cancel that action.
            if (food.CancelToken != null)
            {
                return true;
            }

            if (food.Owner == user || //Suppresses self-eating
                EntityManager.TryGetComponent<MobStateComponent>(food.Owner, out var mobState) && _mobStateSystem.IsAlive(food.Owner, mobState)) // Suppresses eating alive mobs
                return false;

            // Target can't be fed
            if (!EntityManager.HasComponent<BodyComponent>(target))
                return false;

            if (!_solutionContainerSystem.TryGetSolution(food.Owner, food.SolutionName, out var foodSolution))
                return false;

            var flavors = _flavorProfileSystem.GetLocalizedFlavorsMessage(food.Owner, user, foodSolution);

            if (food.UsesRemaining <= 0)
            {
                _popupSystem.PopupEntity(Loc.GetString("food-system-try-use-food-is-empty",
                    ("entity", food.Owner)), user, user);
                DeleteAndSpawnTrash(food, user);
                return false;
            }

            if (IsMouthBlocked(target, user))
                return false;

            if (!TryGetRequiredUtensils(user, food, out var utensils))
                return false;

            if (!_interactionSystem.InRangeUnobstructed(user, food.Owner, popup: true))
                return true;

            var forceFeed = user != target;
            food.CancelToken = new CancellationTokenSource();

            if (forceFeed)
            {
                var userName = Identity.Entity(user, EntityManager);
                _popupSystem.PopupEntity(Loc.GetString("food-system-force-feed", ("user", userName)),
                    user, target);

                // logging
                _adminLogger.Add(LogType.ForceFeed, LogImpact.Medium, $"{ToPrettyString(user):user} is forcing {ToPrettyString(target):target} to eat {ToPrettyString(food.Owner):food} {SolutionContainerSystem.ToPrettyString(foodSolution)}");
            }
            else
            {
                // log voluntary eating
                _adminLogger.Add(LogType.Ingestion, LogImpact.Low, $"{ToPrettyString(target):target} is eating {ToPrettyString(food.Owner):food} {SolutionContainerSystem.ToPrettyString(foodSolution)}");
            }

            var moveBreak = user != target;

            _doAfterSystem.DoAfter(new DoAfterEventArgs(user, forceFeed ? food.ForceFeedDelay : food.Delay, food.CancelToken.Token, target, food.Owner)
            {
                BreakOnUserMove = moveBreak,
                BreakOnDamage = true,
                BreakOnStun = true,
                BreakOnTargetMove = moveBreak,
                MovementThreshold = 0.01f,
                DistanceThreshold = 1.0f,
                TargetFinishedEvent = new FeedEvent(user, food, foodSolution, flavors, utensils),
                BroadcastCancelledEvent = new ForceFeedCancelledEvent(food),
                NeedHand = true,
            });

            return true;

        }

        private void OnFeed(EntityUid uid, BodyComponent body, FeedEvent args)
        {
            if (args.Food.Deleted)
                return;

            args.Food.CancelToken = null;

            if (!_bodySystem.TryGetBodyOrganComponents<StomachComponent>(uid, out var stomachs, body))
                return;

            var transferAmount = args.Food.TransferAmount != null
                ? FixedPoint2.Min((FixedPoint2) args.Food.TransferAmount, args.FoodSolution.CurrentVolume)
                : args.FoodSolution.CurrentVolume;

            var split = _solutionContainerSystem.SplitSolution((args.Food).Owner, args.FoodSolution, transferAmount);


            var firstStomach = stomachs.FirstOrNull(
                stomach => _stomachSystem.CanTransferSolution((stomach.Comp).Owner, split));

            var forceFeed = uid != args.User;

            // No stomach so just popup a message that they can't eat.
            if (firstStomach == null)
            {
                _solutionContainerSystem.TryAddSolution(uid, args.FoodSolution, split);
                _popupSystem.PopupEntity(
                    forceFeed ?
                        Loc.GetString("food-system-you-cannot-eat-any-more-other") :
                        Loc.GetString("food-system-you-cannot-eat-any-more")
                    , uid, args.User);
                return;
            }

            split.DoEntityReaction(uid, ReactionMethod.Ingestion);
            _stomachSystem.TryTransferSolution(firstStomach.Value.Comp.Owner, split, firstStomach.Value.Comp);

            var flavors = args.FlavorMessage;

            if (forceFeed)
            {
                var targetName = Identity.Entity(uid, EntityManager);
                var userName = Identity.Entity(args.User, EntityManager);
                _popupSystem.PopupEntity(Loc.GetString("food-system-force-feed-success", ("user", userName), ("flavors", flavors)),
                    uid, uid);

                _popupSystem.PopupEntity(Loc.GetString("food-system-force-feed-success-user", ("target", targetName)),
                    args.User, args.User);

                // log successful force feed
                _adminLogger.Add(LogType.ForceFeed, LogImpact.Medium, $"{ToPrettyString(uid):user} forced {ToPrettyString(args.User):target} to eat {ToPrettyString(args.Food.Owner):food}");
            }
            else
            {
                _popupSystem.PopupEntity(Loc.GetString(args.Food.EatMessage, ("food", args.Food.Owner), ("flavors", flavors)), args.User, args.User);

                // log successful voluntary eating
                _adminLogger.Add(LogType.Ingestion, LogImpact.Low, $"{ToPrettyString(args.User):target} ate {ToPrettyString(args.Food.Owner):food}");
            }

            SoundSystem.Play(args.Food.UseSound.GetSound(), Filter.Pvs(uid), uid, AudioParams.Default.WithVolume(-1f));

            // Try to break all used utensils
            foreach (var utensil in args.Utensils)
            {
                _utensilSystem.TryBreak((utensil).Owner, args.User);
            }

            if (args.Food.UsesRemaining > 0)
                return;

            if (string.IsNullOrEmpty(args.Food.TrashPrototype))
                EntityManager.QueueDeleteEntity(args.Food.Owner);
            else
                DeleteAndSpawnTrash(args.Food, args.User);
        }

        private void DeleteAndSpawnTrash(FoodComponent component, EntityUid? user = null)
        {
            //We're empty. Become trash.
            var position = Transform(component.Owner).MapPosition;
            var finisher = EntityManager.SpawnEntity(component.TrashPrototype, position);

            // If the user is holding the item
            if (user != null && _handsSystem.IsHolding(user.Value, component.Owner, out var hand))
            {
                EntityManager.DeleteEntity((component).Owner);

                // Put the trash in the user's hand
                _handsSystem.TryPickup(user.Value, finisher, hand);
                return;
            }

            EntityManager.QueueDeleteEntity(component.Owner);
        }

        private void AddEatVerb(EntityUid uid, FoodComponent component, GetVerbsEvent<AlternativeVerb> ev)
        {
            if (component.CancelToken != null)
                return;

            if (uid == ev.User ||
                !ev.CanInteract ||
                !ev.CanAccess ||
                !EntityManager.TryGetComponent(ev.User, out BodyComponent? body) ||
                !_bodySystem.TryGetBodyOrganComponents<StomachComponent>(ev.User, out var stomachs, body))
                return;

            if (EntityManager.TryGetComponent<MobStateComponent>(uid, out var mobState) && _mobStateSystem.IsAlive(uid, mobState))
                return;

            AlternativeVerb verb = new()
            {
                Act = () =>
                {
                    TryFeed(ev.User, ev.User, component);
                },
                IconTexture = "/Textures/Interface/VerbIcons/cutlery.svg.192dpi.png",
                Text = Loc.GetString("food-system-verb-eat"),
                Priority = -1
            };

            ev.Verbs.Add(verb);
        }

        /// <summary>
        ///     Force feeds someone remotely. Does not require utensils (well, not the normal type anyways).
        /// </summary>
        public void ProjectileForceFeed(EntityUid uid, EntityUid target, EntityUid? user, FoodComponent? food = null, BodyComponent? body = null)
        {
            // TODO: Combine with regular feeding because holy code duplication batman.
            if (!Resolve(uid, ref food, false) || !Resolve(target, ref body, false))
                return;

            if (IsMouthBlocked(target))
                return;

            if (!_solutionContainerSystem.TryGetSolution(uid, food.SolutionName, out var foodSolution))
                return;

            if (!_bodySystem.TryGetBodyOrganComponents<StomachComponent>(target, out var stomachs, body))
                return;

            if (food.UsesRemaining <= 0)
                DeleteAndSpawnTrash(food);

            var firstStomach = stomachs.FirstOrNull(
                stomach => _stomachSystem.CanTransferSolution(((IComponent) stomach.Comp).Owner, foodSolution));

            if (firstStomach == null)
                return;

            // logging
            if (user == null)
                _adminLogger.Add(LogType.ForceFeed, $"{ToPrettyString(uid):food} {SolutionContainerSystem.ToPrettyString(foodSolution):solution} was thrown into the mouth of {ToPrettyString(target):target}");
            else
                _adminLogger.Add(LogType.ForceFeed, $"{ToPrettyString(user.Value):user} threw {ToPrettyString(uid):food} {SolutionContainerSystem.ToPrettyString(foodSolution):solution} into the mouth of {ToPrettyString(target):target}");

            var filter = user == null ? Filter.Entities(target) : Filter.Entities(target, user.Value);
            _popupSystem.PopupEntity(Loc.GetString(food.EatMessage, ("food", food.Owner)), target, filter, true);

            foodSolution.DoEntityReaction(uid, ReactionMethod.Ingestion);
            _stomachSystem.TryTransferSolution(((IComponent) firstStomach.Value.Comp).Owner, foodSolution, firstStomach.Value.Comp);
            SoundSystem.Play(food.UseSound.GetSound(), Filter.Pvs(target), target, AudioParams.Default.WithVolume(-1f));

            if (string.IsNullOrEmpty(food.TrashPrototype))
                EntityManager.QueueDeleteEntity(food.Owner);
            else
                DeleteAndSpawnTrash(food);
        }

        private bool TryGetRequiredUtensils(EntityUid user, FoodComponent component,
            out List<UtensilComponent> utensils, HandsComponent? hands = null)
        {
            utensils = new List<UtensilComponent>();

            if (component.Utensil != UtensilType.None)
                return true;

            if (!Resolve(user, ref hands, false))
                return false;

            var usedTypes = UtensilType.None;

            foreach (var item in _handsSystem.EnumerateHeld(user, hands))
            {
                // Is utensil?
                if (!EntityManager.TryGetComponent(item, out UtensilComponent? utensil))
                    continue;

                if ((utensil.Types & component.Utensil) != 0 && // Acceptable type?
                    (usedTypes & utensil.Types) != utensil.Types) // Type is not used already? (removes usage of identical utensils)
                {
                    // Add to used list
                    usedTypes |= utensil.Types;
                    utensils.Add(utensil);
                }
            }

            // If "required" field is set, try to block eating without proper utensils used
            if (component.UtensilRequired && (usedTypes & component.Utensil) != component.Utensil)
            {
                _popupSystem.PopupEntity(Loc.GetString("food-you-need-to-hold-utensil", ("utensil", component.Utensil ^ usedTypes)), user, user);
                return false;
            }

            return true;
        }

        private static void OnFeedCancelled(ForceFeedCancelledEvent args)
        {
            args.Food.CancelToken = null;
        }

        /// <summary>
        ///     Block ingestion attempts based on the equipped mask or head-wear
        /// </summary>
        private void OnInventoryIngestAttempt(EntityUid uid, InventoryComponent component, IngestionAttemptEvent args)
        {
            if (args.Cancelled)
                return;

            IngestionBlockerComponent? blocker;

            if (_inventorySystem.TryGetSlotEntity(uid, "mask", out var maskUid) &&
                EntityManager.TryGetComponent(maskUid, out blocker) &&
                blocker.Enabled)
            {
                args.Blocker = maskUid;
                args.Cancel();
                return;
            }

            if (_inventorySystem.TryGetSlotEntity(uid, "head", out var headUid) &&
                EntityManager.TryGetComponent(headUid, out blocker) &&
                blocker.Enabled)
            {
                args.Blocker = headUid;
                args.Cancel();
            }
        }


        /// <summary>
        ///     Check whether the target's mouth is blocked by equipment (masks or head-wear).
        /// </summary>
        /// <param name="uid">The target whose equipment is checked</param>
        /// <param name="popupUid">Optional entity that will receive an informative pop-up identifying the blocking
        /// piece of equipment.</param>
        /// <returns></returns>
        public bool IsMouthBlocked(EntityUid uid, EntityUid? popupUid = null)
        {
            var attempt = new IngestionAttemptEvent();
            RaiseLocalEvent(uid, attempt, false);
            if (attempt.Cancelled && attempt.Blocker != null && popupUid != null)
            {
                var name = EntityManager.GetComponent<MetaDataComponent>(attempt.Blocker.Value).EntityName;
                _popupSystem.PopupEntity(Loc.GetString("food-system-remove-mask", ("entity", name)),
                    uid, popupUid.Value);
            }

            return attempt.Cancelled;
        }
    }
}
