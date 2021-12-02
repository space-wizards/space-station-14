using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.DoAfter;
using Content.Server.Hands.Components;
using Content.Server.Items;
using Content.Server.Nutrition.Components;
using Content.Server.Popups;
using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
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
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Utility;
using Content.Server.Inventory.Components;
using Content.Shared.Inventory;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Tag;

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

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<FoodComponent, UseInHandEvent>(OnUseFoodInHand);
            SubscribeLocalEvent<FoodComponent, AfterInteractEvent>(OnFeedFood);
            SubscribeLocalEvent<FoodComponent, GetInteractionVerbsEvent>(AddEatVerb);
            SubscribeLocalEvent<SharedBodyComponent, ForceFeedEvent>(OnForceFeed);
            SubscribeLocalEvent<ForceFeedCancelledEvent>(OnForceFeedCancelled);
        }

        /// <summary>
        /// Eat item
        /// </summary>
        private void OnUseFoodInHand(EntityUid uid, FoodComponent foodComponent, UseInHandEvent ev)
        {
            if (ev.Handled)
                return;

            if (!_actionBlockerSystem.CanInteract(ev.UserUid) || !_actionBlockerSystem.CanUse(ev.UserUid))
                return;

            if (!ev.UserUid.InRangeUnobstructed(uid, popup: true))
            {
                ev.Handled = true;
                return;
            }

            ev.Handled = TryUseFood(uid, ev.UserUid);
        }

        /// <summary>
        /// Feed someone else
        /// </summary>
        private void OnFeedFood(EntityUid uid, FoodComponent foodComponent, AfterInteractEvent args)
        {
            if (args.Handled || args.TargetUid == null)
                return;

            if (!_actionBlockerSystem.CanInteract(args.UserUid) || !_actionBlockerSystem.CanUse(args.UserUid))
                return;

            if (!args.UserUid.InRangeUnobstructed(uid, popup: true))
            {
                args.Handled = true;
                return;
            }

            if (args.UserUid == args.TargetUid)
            {
                args.Handled = TryUseFood(uid, args.UserUid);
                return;
            }

            if (!args.UserUid.InRangeUnobstructed(args.TargetUid.Value, popup: true))
            {
                args.Handled = true;
                return;
            }

            args.Handled = TryForceFeed(uid, args.UserUid, args.TargetUid.Value);
        }

        /// <summary>
        /// Tries to eat some food
        /// </summary>
        /// <param name="uid">Food entity.</param>
        /// <param name="userUid">Feeding initiator.</param>
        /// <param name="targetUid">Feeding target.</param>
        /// <returns>True if an interaction occurred (i.e., food was consumed, or a pop-up message was created)</returns>
        public bool TryUseFood(EntityUid uid, EntityUid userUid, FoodComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return false;

            if (uid == userUid || //Suppresses self-eating
                EntityManager.TryGetComponent<MobStateComponent>(uid, out var mobState) && mobState.IsAlive()) // Suppresses eating alive mobs
                return false;

            if (!_solutionContainerSystem.TryGetSolution(uid, component.SolutionName, out var solution))
                return false;

            if (component.UsesRemaining <= 0)
            {
                _popupSystem.PopupEntity(Loc.GetString("food-system-try-use-food-is-empty", ("entity", EntityManager.GetEntity(uid))), userUid, Filter.Entities(userUid));
                DeleteAndSpawnTrash(component, userUid);
                return true;
            }

            if (!EntityManager.TryGetComponent(userUid, out SharedBodyComponent ? body) ||
                !_bodySystem.TryGetComponentsOnMechanisms<StomachComponent>(userUid, out var stomachs, body))
                return false;

            if (IsMouthBlocked(userUid, out var blocker))
            {
                var name = EntityManager.GetComponent<MetaDataComponent>(blocker.Value).EntityName;
                _popupSystem.PopupEntity(Loc.GetString("food-system-remove-mask", ("entity", name)),
                    userUid, Filter.Entities(userUid));
                return true;
            }

            var usedUtensils = new List<UtensilComponent>();

            if (!TryGetRequiredUtensils(userUid, component, out var utensils))
                return true;

            if (!userUid.InRangeUnobstructed(uid, popup: true))
                return true;

            var transferAmount = component.TransferAmount != null ? FixedPoint2.Min((FixedPoint2) component.TransferAmount, solution.CurrentVolume) : solution.CurrentVolume;
            var split = _solutionContainerSystem.SplitSolution(uid, solution, transferAmount);
            var firstStomach = stomachs.FirstOrNull(
                stomach => _stomachSystem.CanTransferSolution(stomach.Comp.OwnerUid, split));

            if (firstStomach == null)
            {
                _solutionContainerSystem.TryAddSolution(uid, solution, split);
                _popupSystem.PopupEntity(Loc.GetString("food-system-you-cannot-eat-any-more"), userUid, Filter.Entities(userUid));
                return true;
            }

            // TODO: Account for partial transfer.
            split.DoEntityReaction(userUid, ReactionMethod.Ingestion);
            _stomachSystem.TryTransferSolution(firstStomach.Value.Comp.OwnerUid, split, firstStomach.Value.Comp);

            SoundSystem.Play(Filter.Pvs(userUid), component.UseSound.GetSound(), userUid, AudioParams.Default.WithVolume(-1f));
            _popupSystem.PopupEntity(Loc.GetString(component.EatMessage, ("food", component.Owner)), userUid, Filter.Entities(userUid));

            // Try to break all used utensils
            foreach (var utensil in usedUtensils)
            {
                _utensilSystem.TryBreak(utensil.OwnerUid, userUid);
            }

            if (component.UsesRemaining > 0)
            {
                return true;
            }

            if (string.IsNullOrEmpty(component.TrashPrototype))
                EntityManager.QueueDeleteEntity(component.OwnerUid);
            else
                DeleteAndSpawnTrash(component, userUid);

            return true;
        }

        private void DeleteAndSpawnTrash(FoodComponent component, EntityUid? userUid = null)
        {
            //We're empty. Become trash.
            var position = component.Owner.Transform.Coordinates;
            var finisher = component.Owner.EntityManager.SpawnEntity(component.TrashPrototype, position);

            // If the user is holding the item
            if (userUid != null &&
                EntityManager.TryGetComponent(userUid.Value, out HandsComponent? handsComponent) &&
                handsComponent.IsHolding(component.Owner))
            {
                EntityManager.DeleteEntity(component.OwnerUid);

                // Put the trash in the user's hand
                if (finisher.TryGetComponent(out ItemComponent? item) &&
                    handsComponent.CanPutInHand(item))
                {
                    handsComponent.PutInHand(item);
                }
                return;
            }

            EntityManager.QueueDeleteEntity(component.OwnerUid);
        }

        private void AddEatVerb(EntityUid uid, FoodComponent component, GetInteractionVerbsEvent ev)
        {
            if (uid == ev.UserUid ||
                !ev.CanInteract ||
                !ev.CanAccess ||
                !EntityManager.TryGetComponent(ev.UserUid, out SharedBodyComponent? body) ||
                !_bodySystem.TryGetComponentsOnMechanisms<StomachComponent>(ev.UserUid, out var stomachs, body))
                return;

            if (EntityManager.TryGetComponent<MobStateComponent>(uid, out var mobState) && mobState.IsAlive())
                return;

            Verb verb = new();
            verb.Act = () =>
            {
                TryUseFood(uid, ev.UserUid, component);
            };

            verb.Text = Loc.GetString("food-system-verb-eat");
            verb.Priority = -1;
            ev.Verbs.Add(verb);
        }


        /// <summary>
        ///     Attempts to force feed a target. Returns true if any interaction occurred, including pop-up generation
        /// </summary>
        public bool TryForceFeed(EntityUid uid, EntityUid userUid, EntityUid targetUid, FoodComponent? food = null)
        {
            if (!Resolve(uid, ref food))
                return false;

            if (!EntityManager.HasComponent<SharedBodyComponent>(targetUid))
                return false;

            if (!_solutionContainerSystem.TryGetSolution(uid, food.SolutionName, out var foodSolution))
                return false;

            if (food.UsesRemaining <= 0)
            {
                _popupSystem.PopupEntity(Loc.GetString("food-system-try-use-food-is-empty",
                    ("entity", EntityManager.GetEntity(uid))), userUid, Filter.Entities(userUid));
                DeleteAndSpawnTrash(food, userUid);
                return true;
            }

            if (IsMouthBlocked(targetUid, out var blocker))
            {
                var name = EntityManager.GetComponent<MetaDataComponent>(blocker.Value).EntityName;
                _popupSystem.PopupEntity(Loc.GetString("food-system-remove-mask", ("entity", name)),
                    userUid, Filter.Entities(userUid));
                return true;
            }

            if (!TryGetRequiredUtensils(userUid, food, out var utensils))
                return true;

            EntityManager.TryGetComponent(userUid, out MetaDataComponent? meta);
            var userName = meta?.EntityName ?? string.Empty;

            _popupSystem.PopupEntity(Loc.GetString("food-system-force-feed", ("user", userName)),
                userUid, Filter.Entities(targetUid));

            _doAfterSystem.DoAfter(new DoAfterEventArgs(userUid, food.ForceFeedDelay, target: targetUid)
            {
                BreakOnUserMove = true,
                BreakOnDamage = true,
                BreakOnStun = true,
                BreakOnTargetMove = true,
                MovementThreshold = 1.0f,
                TargetFinishedEvent = new ForceFeedEvent(userUid, food, foodSolution, utensils),
                BroadcastCancelledEvent = new ForceFeedCancelledEvent(food)
            });

            // logging
            var user = EntityManager.GetEntity(userUid);
            var target = EntityManager.GetEntity(targetUid);
            var edible = EntityManager.GetEntity(uid);
            _logSystem.Add(LogType.ForceFeed, LogImpact.Medium, $"{user} is forcing {target} to eat {edible}");

            food.InUse = true;
            return true;
        }

        private void OnForceFeed(EntityUid uid, SharedBodyComponent body, ForceFeedEvent args)
        {
            args.Food.InUse = false;

            if (!_bodySystem.TryGetComponentsOnMechanisms<StomachComponent>(uid, out var stomachs, body))
                return;

            var transferAmount = args.Food.TransferAmount != null
                ? FixedPoint2.Min((FixedPoint2) args.Food.TransferAmount, args.FoodSolution.CurrentVolume)
                : args.FoodSolution.CurrentVolume;

            var split = _solutionContainerSystem.SplitSolution(args.Food.OwnerUid, args.FoodSolution, transferAmount);
            var firstStomach = stomachs.FirstOrNull(
                stomach => _stomachSystem.CanTransferSolution(stomach.Comp.OwnerUid, split));

            if (firstStomach == null)
            {
                _solutionContainerSystem.TryAddSolution(uid, args.FoodSolution, split);
                _popupSystem.PopupEntity(Loc.GetString("food-system-you-cannot-eat-any-more-other"), uid, Filter.Entities(args.User));
                return;
            }

            split.DoEntityReaction(uid, ReactionMethod.Ingestion);
            _stomachSystem.TryTransferSolution(firstStomach.Value.Comp.OwnerUid, split, firstStomach.Value.Comp);

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
                _utensilSystem.TryBreak(utensil.OwnerUid, args.User);
            }

            if (args.Food.UsesRemaining > 0)
                return;

            if (string.IsNullOrEmpty(args.Food.TrashPrototype))
                EntityManager.QueueDeleteEntity(args.Food.OwnerUid);
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

            if (IsMouthBlocked(target, out _))
                return;

            if (!_solutionContainerSystem.TryGetSolution(uid, food.SolutionName, out var foodSolution))
                return;

            if (!_bodySystem.TryGetComponentsOnMechanisms<StomachComponent>(target, out var stomachs, body))
                return;

            if (food.UsesRemaining <= 0)
                DeleteAndSpawnTrash(food);

            var firstStomach = stomachs.FirstOrNull(
                stomach => _stomachSystem.CanTransferSolution(stomach.Comp.OwnerUid, foodSolution));

            if (firstStomach == null)
                return;

            // logging
            var userEntity = (user == null) ? null : EntityManager.GetEntity(user.Value);
            var targetEntity = EntityManager.GetEntity(target);
            var edible = EntityManager.GetEntity(uid);
            if (userEntity == null)
                _logSystem.Add(LogType.ForceFeed, $"{edible} was thrown into the mouth of {targetEntity}");
            else
                _logSystem.Add(LogType.ForceFeed, $"{userEntity} threw {edible} into the mouth of {targetEntity}");

            var filter = (user == null) ? Filter.Entities(target) : Filter.Entities(target, user.Value);
            _popupSystem.PopupEntity(Loc.GetString(food.EatMessage), target, filter);

            foodSolution.DoEntityReaction(uid, ReactionMethod.Ingestion);
            _stomachSystem.TryTransferSolution(firstStomach.Value.Comp.OwnerUid, foodSolution, firstStomach.Value.Comp);
            SoundSystem.Play(Filter.Pvs(target), food.UseSound.GetSound(), target, AudioParams.Default.WithVolume(-1f));

            if (string.IsNullOrEmpty(food.TrashPrototype))
                EntityManager.QueueDeleteEntity(food.OwnerUid);
            else
                DeleteAndSpawnTrash(food);
        }

        private bool TryGetRequiredUtensils(EntityUid userUid, FoodComponent component,
            out List<UtensilComponent> utensils, HandsComponent? hands = null)
        {
            utensils = new();

            if (component.Utensil != UtensilType.None)
                return true;

            if (!Resolve(userUid, ref hands, false))
                return false;

            var usedTypes = UtensilType.None;

            foreach (var item in hands.GetAllHeldItems())
            {
                // Is utensil?
                if (!item.Owner.TryGetComponent(out UtensilComponent? utensil))
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
                _popupSystem.PopupEntity(Loc.GetString("food-you-need-to-hold-utensil", ("utensil", component.Utensil ^ usedTypes)), userUid, Filter.Entities(userUid));
                return false;
            }

            return true;
        }

        private void OnForceFeedCancelled(ForceFeedCancelledEvent args)
        {
            args.Food.InUse = false;
        }

        /// <summary>
        ///     Is an entity's mouth accessible, or is it blocked by something like a mask? Does not actually check if
        ///     the user has a mouth. Body system when?
        /// </summary>
        public bool IsMouthBlocked(EntityUid uid, [NotNullWhen(true)] out EntityUid? blockingEntity,
            InventoryComponent? inventory = null)
        {
            blockingEntity = null;

            if (!Resolve(uid, ref inventory, false))
                return false;

            // check masks
            if (inventory.TryGetSlotItem(EquipmentSlotDefines.Slots.MASK, out ItemComponent? mask))
            {
                // For now, lets just assume that any masks always covers the mouth
                // TODO MASKS if the ability is added to raise/lower masks, this needs to be updated.
                blockingEntity = mask.OwnerUid;
                return true;
            }

            // check helmets. Note that not all helmets cover the face.
            if (inventory.TryGetSlotItem(EquipmentSlotDefines.Slots.HEAD, out ItemComponent? head) &&
                EntityManager.TryGetComponent(head.OwnerUid, out TagComponent tag) &&
                tag.HasTag("ConcealsFace"))
            {
                blockingEntity = head.OwnerUid;
                return true;
            }

            return false;
        }
    }

    public sealed class ForceFeedEvent : EntityEventArgs
    {
        public readonly EntityUid User;
        public readonly FoodComponent Food;
        public readonly Solution FoodSolution;
        public readonly List<UtensilComponent> Utensils;

        public ForceFeedEvent(EntityUid user, FoodComponent food, Solution foodSolution, List<UtensilComponent> utensils)
        {
            User = user;
            Food = food;
            FoodSolution = foodSolution;
            Utensils = utensils;
        }
    }

    public sealed class ForceFeedCancelledEvent : EntityEventArgs
    {
        public readonly FoodComponent Food;

        public ForceFeedCancelledEvent(FoodComponent food)
        {
            Food = food;
        }
    }
}
