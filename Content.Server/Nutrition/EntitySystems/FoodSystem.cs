using System.Collections.Generic;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.DoAfter;
using Content.Server.Hands.Components;
using Content.Server.Nutrition.Components;
using Content.Server.Popups;
using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.MobState.Components;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using Content.Shared.Hands;
using Content.Shared.Inventory;
using Content.Shared.Item;

namespace Content.Server.Nutrition.EntitySystems
{
    /// <summary>
    /// Handles feeding attempts both on yourself and on the target.
    /// </summary>
    internal class FoodSystem : EntitySystem
    {
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly BodySystem _bodySystem = default!;
        [Dependency] private readonly StomachSystem _stomachSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly UtensilSystem _utensilSystem = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly SharedAdminLogSystem _logSystem = default!;
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] private readonly InventorySystem _inventorySystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<FoodComponent, UseInHandEvent>(OnUseFoodInHand);
            SubscribeLocalEvent<FoodComponent, AfterInteractEvent>(OnFeedFood);
            SubscribeLocalEvent<FoodComponent, HandDeselectedEvent>(OnFoodDeselected);
            SubscribeLocalEvent<FoodComponent, GetInteractionVerbsEvent>(AddEatVerb);
            SubscribeLocalEvent<SharedBodyComponent, ForceFeedEvent>(OnForceFeed);
            SubscribeLocalEvent<ForceFeedCancelledEvent>(OnForceFeedCancelled);
            SubscribeLocalEvent<InventoryComponent, IngestionAttemptEvent>(OnInventoryIngestAttempt);
        }

        /// <summary>
        ///     If the user is currently force feeding someone, this cancels the attempt if they swap hands or otherwise
        ///     loose the item. Prevents force-feeding dual-wielding.
        /// </summary>
        private void OnFoodDeselected(EntityUid uid, FoodComponent component, HandDeselectedEvent args)
        {
            if (component.CancelToken != null)
            {
                component.CancelToken.Cancel();
                component.CancelToken = null;
            }
        }

        /// <summary>
        /// Eat item
        /// </summary>
        private void OnUseFoodInHand(EntityUid uid, FoodComponent foodComponent, UseInHandEvent ev)
        {
            if (ev.Handled)
                return;

            if (!_actionBlockerSystem.CanInteract(ev.User) || !_actionBlockerSystem.CanUse(ev.User))
                return;

            if (!ev.User.InRangeUnobstructed(uid, popup: true))
            {
                ev.Handled = true;
                return;
            }

            ev.Handled = TryUseFood(uid, ev.User);
        }

        /// <summary>
        /// Feed someone else
        /// </summary>
        private void OnFeedFood(EntityUid uid, FoodComponent foodComponent, AfterInteractEvent args)
        {
            if (args.Handled || args.Target == null)
                return;

            if (!_actionBlockerSystem.CanInteract(args.User) || !_actionBlockerSystem.CanUse(args.User))
                return;

            if (!args.User.InRangeUnobstructed(uid, popup: true))
            {
                args.Handled = true;
                return;
            }

            if (args.User == args.Target)
            {
                args.Handled = TryUseFood(uid, args.User);
                return;
            }

            if (!args.User.InRangeUnobstructed(args.Target.Value, popup: true))
            {
                args.Handled = true;
                return;
            }

            args.Handled = TryForceFeed(uid, args.User, args.Target.Value);
        }

        /// <summary>
        /// Tries to eat some food
        /// </summary>
        /// <param name="uid">Food entity.</param>
        /// <param name="user">Feeding initiator.</param>
        /// <returns>True if an interaction occurred (i.e., food was consumed, or a pop-up message was created)</returns>
        public bool TryUseFood(EntityUid uid, EntityUid user, FoodComponent? food = null)
        {
            if (!Resolve(uid, ref food))
                return false;

            // if currently being used to force-feed, cancel that action.
            if (food.CancelToken != null)
            {
                food.CancelToken.Cancel();
                food.CancelToken = null;
                return true;
            }

            if (uid == user || //Suppresses self-eating
                EntityManager.TryGetComponent<MobStateComponent>(uid, out var mobState) && mobState.IsAlive()) // Suppresses eating alive mobs
                return false;

            if (!_solutionContainerSystem.TryGetSolution(uid, food.SolutionName, out var solution))
                return false;

            if (food.UsesRemaining <= 0)
            {
                _popupSystem.PopupEntity(Loc.GetString("food-system-try-use-food-is-empty", ("entity", uid)), user, Filter.Entities(user));
                DeleteAndSpawnTrash(food, user);
                return true;
            }

            if (!EntityManager.TryGetComponent(user, out SharedBodyComponent ? body) ||
                !_bodySystem.TryGetComponentsOnMechanisms<StomachComponent>(user, out var stomachs, body))
                return false;

            if (IsMouthBlocked(user, user))
            {
                return true;
            }

            var usedUtensils = new List<UtensilComponent>();

            if (!TryGetRequiredUtensils(user, food, out var utensils))
                return true;

            if (!user.InRangeUnobstructed(uid, popup: true))
                return true;

            var transferAmount = food.TransferAmount != null ? FixedPoint2.Min((FixedPoint2) food.TransferAmount, solution.CurrentVolume) : solution.CurrentVolume;
            var split = _solutionContainerSystem.SplitSolution(uid, solution, transferAmount);
            var firstStomach = stomachs.FirstOrNull(
                stomach => _stomachSystem.CanTransferSolution((stomach.Comp).Owner, split));

            if (firstStomach == null)
            {
                _solutionContainerSystem.TryAddSolution(uid, solution, split);
                _popupSystem.PopupEntity(Loc.GetString("food-system-you-cannot-eat-any-more"), user, Filter.Entities(user));
                return true;
            }

            // TODO: Account for partial transfer.
            split.DoEntityReaction(user, ReactionMethod.Ingestion);
            _stomachSystem.TryTransferSolution((firstStomach.Value.Comp).Owner, split, firstStomach.Value.Comp);

            SoundSystem.Play(Filter.Pvs(user), food.UseSound.GetSound(), user, AudioParams.Default.WithVolume(-1f));
            _popupSystem.PopupEntity(Loc.GetString(food.EatMessage, ("food", food.Owner)), user, Filter.Entities(user));

            // Try to break all used utensils
            foreach (var utensil in usedUtensils)
            {
                _utensilSystem.TryBreak((utensil).Owner, user);
            }

            if (food.UsesRemaining > 0)
            {
                return true;
            }

            if (string.IsNullOrEmpty(food.TrashPrototype))
                EntityManager.QueueDeleteEntity((food).Owner);
            else
                DeleteAndSpawnTrash(food, user);

            return true;
        }

        private void DeleteAndSpawnTrash(FoodComponent component, EntityUid? user = null)
        {
            //We're empty. Become trash.
            var position = EntityManager.GetComponent<TransformComponent>(component.Owner).Coordinates;
            var finisher = EntityManager.SpawnEntity(component.TrashPrototype, position);

            // If the user is holding the item
            if (user != null &&
                EntityManager.TryGetComponent(user.Value, out HandsComponent? handsComponent) &&
                handsComponent.IsHolding(component.Owner))
            {
                EntityManager.DeleteEntity((component).Owner);

                // Put the trash in the user's hand
                if (EntityManager.TryGetComponent(finisher, out SharedItemComponent? item) &&
                    handsComponent.CanPutInHand(item))
                {
                    handsComponent.PutInHand(item);
                }
                return;
            }

            EntityManager.QueueDeleteEntity((component).Owner);
        }

        private void AddEatVerb(EntityUid uid, FoodComponent component, GetInteractionVerbsEvent ev)
        {
            if (component.CancelToken != null)
                return;

            if (uid == ev.User ||
                !ev.CanInteract ||
                !ev.CanAccess ||
                !EntityManager.TryGetComponent(ev.User, out SharedBodyComponent? body) ||
                !_bodySystem.TryGetComponentsOnMechanisms<StomachComponent>(ev.User, out var stomachs, body))
                return;

            if (EntityManager.TryGetComponent<MobStateComponent>(uid, out var mobState) && mobState.IsAlive())
                return;

            Verb verb = new()
            {
                Act = () =>
                {
                    TryUseFood(uid, ev.User, component);
                },
                Text = Loc.GetString("food-system-verb-eat"),
                Priority = -1
            };

            ev.Verbs.Add(verb);
        }


        /// <summary>
        ///     Attempts to force feed a target. Returns true if any interaction occurred, including pop-up generation
        /// </summary>
        public bool TryForceFeed(EntityUid uid, EntityUid user, EntityUid target, FoodComponent? food = null)
        {
            if (!Resolve(uid, ref food))
                return false;

            // if currently being used to force-feed, cancel that action.
            if (food.CancelToken != null)
            {
                food.CancelToken.Cancel();
                food.CancelToken = null;
                return true;
            }

            if (!EntityManager.HasComponent<SharedBodyComponent>(target))
                return false;

            if (!_solutionContainerSystem.TryGetSolution(uid, food.SolutionName, out var foodSolution))
                return false;

            if (food.UsesRemaining <= 0)
            {
                _popupSystem.PopupEntity(Loc.GetString("food-system-try-use-food-is-empty",
                    ("entity", uid)), user, Filter.Entities(user));
                DeleteAndSpawnTrash(food, user);
                return true;
            }

            if (IsMouthBlocked(target, user))
            {
                return true;
            }

            if (!TryGetRequiredUtensils(user, food, out var utensils))
                return true;

            EntityManager.TryGetComponent(user, out MetaDataComponent? meta);
            var userName = meta?.EntityName ?? string.Empty;

            _popupSystem.PopupEntity(Loc.GetString("food-system-force-feed", ("user", userName)),
                user, Filter.Entities(target));

            food.CancelToken = new();
            _doAfterSystem.DoAfter(new DoAfterEventArgs(user, food.ForceFeedDelay, food.CancelToken.Token, target)
            {
                BreakOnUserMove = true,
                BreakOnDamage = true,
                BreakOnStun = true,
                BreakOnTargetMove = true,
                MovementThreshold = 1.0f,
                TargetFinishedEvent = new ForceFeedEvent(user, food, foodSolution, utensils),
                BroadcastCancelledEvent = new ForceFeedCancelledEvent(food)
            });

            // logging
            _logSystem.Add(LogType.ForceFeed, LogImpact.Medium, $"{ToPrettyString(user):user} is forcing {ToPrettyString(target):target} to eat {ToPrettyString(uid):food} {SolutionContainerSystem.ToPrettyString(foodSolution):solution}");

            return true;
        }

        private void OnForceFeed(EntityUid uid, SharedBodyComponent body, ForceFeedEvent args)
        {
            if (args.Food.Deleted)
                return;

            args.Food.CancelToken = null;

            if (!_bodySystem.TryGetComponentsOnMechanisms<StomachComponent>(uid, out var stomachs, body))
                return;

            var transferAmount = args.Food.TransferAmount != null
                ? FixedPoint2.Min((FixedPoint2) args.Food.TransferAmount, args.FoodSolution.CurrentVolume)
                : args.FoodSolution.CurrentVolume;

            var split = _solutionContainerSystem.SplitSolution((args.Food).Owner, args.FoodSolution, transferAmount);
            var firstStomach = stomachs.FirstOrNull(
                stomach => _stomachSystem.CanTransferSolution((stomach.Comp).Owner, split));

            if (firstStomach == null)
            {
                _solutionContainerSystem.TryAddSolution(uid, args.FoodSolution, split);
                _popupSystem.PopupEntity(Loc.GetString("food-system-you-cannot-eat-any-more-other"), uid, Filter.Entities(args.User));
                return;
            }

            split.DoEntityReaction(uid, ReactionMethod.Ingestion);
            _stomachSystem.TryTransferSolution((firstStomach.Value.Comp).Owner, split, firstStomach.Value.Comp);

            EntityManager.TryGetComponent(uid, out MetaDataComponent? targetMeta);
            var targetName = targetMeta?.EntityName ?? string.Empty;

            EntityManager.TryGetComponent(args.User, out MetaDataComponent? userMeta);
            var userName = userMeta?.EntityName ?? string.Empty;

            _popupSystem.PopupEntity(Loc.GetString("food-system-force-feed-success", ("user", userName)),
                uid, Filter.Entities(uid));

            _popupSystem.PopupEntity(Loc.GetString("food-system-force-feed-success-user", ("target", targetName)),
                args.User, Filter.Entities(args.User));

            SoundSystem.Play(Filter.Pvs(uid), args.Food.UseSound.GetSound(), uid, AudioParams.Default.WithVolume(-1f));

            // Try to break all used utensils
            foreach (var utensil in args.Utensils)
            {
                _utensilSystem.TryBreak((utensil).Owner, args.User);
            }

            if (args.Food.UsesRemaining > 0)
                return;

            if (string.IsNullOrEmpty(args.Food.TrashPrototype))
                EntityManager.QueueDeleteEntity((args.Food).Owner);
            else
                DeleteAndSpawnTrash(args.Food, args.User);
        }

        /// <summary>
        ///     Force feeds someone remotely. Does not require utensils (well, not the normal type anyways).
        /// </summary>
        public void ProjectileForceFeed(EntityUid uid, EntityUid target, EntityUid? user, FoodComponent? food = null, BodyComponent? body = null)
        {
            if (!Resolve(uid, ref food) || !Resolve(target, ref body, false))
                return;

            if (IsMouthBlocked(target))
                return;

            if (!_solutionContainerSystem.TryGetSolution(uid, food.SolutionName, out var foodSolution))
                return;

            if (!_bodySystem.TryGetComponentsOnMechanisms<StomachComponent>(target, out var stomachs, body))
                return;

            if (food.UsesRemaining <= 0)
                DeleteAndSpawnTrash(food);

            var firstStomach = stomachs.FirstOrNull(
                stomach => _stomachSystem.CanTransferSolution(((IComponent) stomach.Comp).Owner, foodSolution));

            if (firstStomach == null)
                return;

            // logging
            if (user == null)
                _logSystem.Add(LogType.ForceFeed, $"{ToPrettyString(uid):food} {SolutionContainerSystem.ToPrettyString(foodSolution):solution} was thrown into the mouth of {ToPrettyString(target):target}");
            else
                _logSystem.Add(LogType.ForceFeed, $"{ToPrettyString(user.Value):user} threw {ToPrettyString(uid):food} {SolutionContainerSystem.ToPrettyString(foodSolution):solution} into the mouth of {ToPrettyString(target):target}");

            var filter = user == null ? Filter.Entities(target) : Filter.Entities(target, user.Value);
            _popupSystem.PopupEntity(Loc.GetString(food.EatMessage), target, filter);

            foodSolution.DoEntityReaction(uid, ReactionMethod.Ingestion);
            _stomachSystem.TryTransferSolution(((IComponent) firstStomach.Value.Comp).Owner, foodSolution, firstStomach.Value.Comp);
            SoundSystem.Play(Filter.Pvs(target), food.UseSound.GetSound(), target, AudioParams.Default.WithVolume(-1f));

            if (string.IsNullOrEmpty(food.TrashPrototype))
                EntityManager.QueueDeleteEntity(((IComponent) food).Owner);
            else
                DeleteAndSpawnTrash(food);
        }

        private bool TryGetRequiredUtensils(EntityUid user, FoodComponent component,
            out List<UtensilComponent> utensils, HandsComponent? hands = null)
        {
            utensils = new();

            if (component.Utensil != UtensilType.None)
                return true;

            if (!Resolve(user, ref hands, false))
                return false;

            var usedTypes = UtensilType.None;

            foreach (var item in hands.GetAllHeldItems())
            {
                // Is utensil?
                if (!EntityManager.TryGetComponent(item.Owner, out UtensilComponent? utensil))
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
                _popupSystem.PopupEntity(Loc.GetString("food-you-need-to-hold-utensil", ("utensil", component.Utensil ^ usedTypes)), user, Filter.Entities(user));
                return false;
            }

            return true;
        }

        private void OnForceFeedCancelled(ForceFeedCancelledEvent args)
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

            IngestionBlockerComponent blocker;

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
                    uid, Filter.Entities(popupUid.Value));
            }

            return attempt.Cancelled;
        }
    }
}
