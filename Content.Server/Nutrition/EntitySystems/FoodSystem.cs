using System.Linq;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Nutrition.Components;
using Content.Server.Popups;
using Content.Server.Stack;
using Content.Shared.Administration.Logs;
using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition;
using Content.Shared.Verbs;
using Content.Shared.Stacks;
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
        [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
        [Dependency] private readonly InventorySystem _inventorySystem = default!;
        [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
        [Dependency] private readonly ReactiveSystem _reaction = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly StackSystem _stack = default!;

        public const float MaxFeedDistance = 1.0f;

        public override void Initialize()
        {
            base.Initialize();

            // TODO add InteractNoHandEvent for entities like mice.
            SubscribeLocalEvent<FoodComponent, UseInHandEvent>(OnUseFoodInHand);
            SubscribeLocalEvent<FoodComponent, AfterInteractEvent>(OnFeedFood);
            SubscribeLocalEvent<FoodComponent, GetVerbsEvent<AlternativeVerb>>(AddEatVerb);
            SubscribeLocalEvent<FoodComponent, ConsumeDoAfterEvent>(OnDoAfter);
            SubscribeLocalEvent<InventoryComponent, IngestionAttemptEvent>(OnInventoryIngestAttempt);
        }

        /// <summary>
        /// Eat item
        /// </summary>
        private void OnUseFoodInHand(EntityUid uid, FoodComponent foodComponent, UseInHandEvent ev)
        {
            if (ev.Handled)
                return;

            var result = TryFeed(ev.User, ev.User, uid, foodComponent);
            ev.Handled = result.Handled;
        }

        /// <summary>
        /// Feed someone else
        /// </summary>
        private void OnFeedFood(EntityUid uid, FoodComponent foodComponent, AfterInteractEvent args)
        {
            if (args.Handled || args.Target == null || !args.CanReach)
                return;

            var result = TryFeed(args.User, args.Target.Value, uid, foodComponent);
            args.Handled = result.Handled;
        }

        public (bool Success, bool Handled) TryFeed(EntityUid user, EntityUid target, EntityUid food, FoodComponent foodComp)
        {
            //Suppresses self-eating
            if (food == user || TryComp<MobStateComponent>(food, out var mobState) && _mobStateSystem.IsAlive(food, mobState)) // Suppresses eating alive mobs
                return (false, false);

            // Target can't be fed or they're already eating
            if (!TryComp<BodyComponent>(target, out var body))
                return (false, false);

            if (!_solutionContainerSystem.TryGetSolution(food, foodComp.SolutionName, out var foodSolution) || foodSolution.Name == null)
                return (false, false);

            if (!_bodySystem.TryGetBodyOrganComponents<StomachComponent>(target, out var stomachs, body))
                return (false, false);

            var forceFeed = user != target;

            if (!IsDigestibleBy(food, foodComp, stomachs))
            {
                _popupSystem.PopupEntity(
                    forceFeed
                        ? Loc.GetString("food-system-cant-digest-other", ("entity", food))
                        : Loc.GetString("food-system-cant-digest", ("entity", food)), user, user);
                return (false, true);
            }

            var flavors = _flavorProfileSystem.GetLocalizedFlavorsMessage(food, user, foodSolution);

            if (foodComp.UsesRemaining <= 0)
            {
                _popupSystem.PopupEntity(Loc.GetString("food-system-try-use-food-is-empty", ("entity", food)), user, user);
                DeleteAndSpawnTrash(foodComp, food, user);
                return (false, true);
            }

            if (IsMouthBlocked(target, user))
                return (false, true);

            if (!_interactionSystem.InRangeUnobstructed(user, food, popup: true))
                return (false, true);

            if (!_interactionSystem.InRangeUnobstructed(user, target, MaxFeedDistance, popup: true))
                return (false, true);

            // TODO make do-afters account for fixtures in the range check.
            if (!Transform(user).MapPosition.InRange(Transform(target).MapPosition, MaxFeedDistance))
            {
                var message = Loc.GetString("interaction-system-user-interaction-cannot-reach");
                _popupSystem.PopupEntity(message, user, user);
                return (false, true);
            }

            if (!TryGetRequiredUtensils(user, foodComp, out _))
                return (false, true);

            if (forceFeed)
            {
                var userName = Identity.Entity(user, EntityManager);
                _popupSystem.PopupEntity(Loc.GetString("food-system-force-feed", ("user", userName)),
                    user, target);

                // logging
                _adminLogger.Add(LogType.ForceFeed, LogImpact.Medium, $"{ToPrettyString(user):user} is forcing {ToPrettyString(target):target} to eat {ToPrettyString(food):food} {SolutionContainerSystem.ToPrettyString(foodSolution)}");
            }
            else
            {
                // log voluntary eating
                _adminLogger.Add(LogType.Ingestion, LogImpact.Low, $"{ToPrettyString(target):target} is eating {ToPrettyString(food):food} {SolutionContainerSystem.ToPrettyString(foodSolution)}");
            }

            var doAfterArgs = new DoAfterArgs(
                user,
                forceFeed ? foodComp.ForceFeedDelay : foodComp.Delay,
                new ConsumeDoAfterEvent(foodSolution.Name, flavors),
                eventTarget: food,
                target: target,
                used: food)
            {
                BreakOnUserMove = forceFeed,
                BreakOnDamage = true,
                BreakOnTargetMove = forceFeed,
                MovementThreshold = 0.01f,
                DistanceThreshold = MaxFeedDistance,
                // Mice and the like can eat without hands.
                // TODO maybe set this based on some CanEatWithoutHands event or component?
                NeedHand = forceFeed,
            };

            _doAfterSystem.TryStartDoAfter(doAfterArgs);
            return (true, true);
        }

        private void OnDoAfter(EntityUid uid, FoodComponent component, ConsumeDoAfterEvent args)
        {
            if (args.Cancelled || args.Handled || component.Deleted || args.Target == null)
                return;

            if (!TryComp<BodyComponent>(args.Target.Value, out var body))
                return;

            if (!_bodySystem.TryGetBodyOrganComponents<StomachComponent>(args.Target.Value, out var stomachs, body))
                return;

            if (!_solutionContainerSystem.TryGetSolution(args.Used, args.Solution, out var solution))
                return;

            if (!TryGetRequiredUtensils(args.User, component, out var utensils))
                return;

            // TODO this should really be checked every tick.
            if (IsMouthBlocked(args.Target.Value))
                return;

            // TODO this should really be checked every tick.
            if (!_interactionSystem.InRangeUnobstructed(args.User, args.Target.Value))
                return;

            var forceFeed = args.User != args.Target;

            args.Handled = true;
            var transferAmount = component.TransferAmount != null ? FixedPoint2.Min((FixedPoint2) component.TransferAmount, solution.Volume) : solution.Volume;

            var split = _solutionContainerSystem.SplitSolution(uid, solution, transferAmount);

            //TODO: Get the stomach UID somehow without nabbing owner
            // Get the stomach with the highest available solution volume
            var highestAvailable = FixedPoint2.Zero;
            StomachComponent? stomachToUse = null;
            foreach (var (stomach, _) in stomachs)
            {
                var owner = stomach.Owner;
                if (!_stomachSystem.CanTransferSolution(owner, split))
                    continue;

                if (!_solutionContainerSystem.TryGetSolution(owner, StomachSystem.DefaultSolutionName,
                        out var stomachSol))
                    continue;

                if (stomachSol.AvailableVolume <= highestAvailable)
                    continue;

                stomachToUse = stomach;
                highestAvailable = stomachSol.AvailableVolume;
            }

            // No stomach so just popup a message that they can't eat.
            if (stomachToUse == null)
            {
                _solutionContainerSystem.TryAddSolution(uid, solution, split);
                _popupSystem.PopupEntity(forceFeed ? Loc.GetString("food-system-you-cannot-eat-any-more-other") : Loc.GetString("food-system-you-cannot-eat-any-more"), args.Target.Value, args.User);
                return;
            }

            _reaction.DoEntityReaction(args.Target.Value, solution, ReactionMethod.Ingestion);
            _stomachSystem.TryTransferSolution(stomachToUse.Owner, split, stomachToUse);

            var flavors = args.FlavorMessage;

            if (forceFeed)
            {
                var targetName = Identity.Entity(args.Target.Value, EntityManager);
                var userName = Identity.Entity(args.User, EntityManager);
                _popupSystem.PopupEntity(Loc.GetString("food-system-force-feed-success", ("user", userName), ("flavors", flavors)),
                    uid, uid);

                _popupSystem.PopupEntity(Loc.GetString("food-system-force-feed-success-user", ("target", targetName)), args.User, args.User);

                // log successful force feed
                _adminLogger.Add(LogType.ForceFeed, LogImpact.Medium, $"{ToPrettyString(uid):user} forced {ToPrettyString(args.User):target} to eat {ToPrettyString(uid):food}");
            }
            else
            {
                _popupSystem.PopupEntity(Loc.GetString(component.EatMessage, ("food", uid), ("flavors", flavors)), args.User, args.User);

                // log successful voluntary eating
                _adminLogger.Add(LogType.Ingestion, LogImpact.Low, $"{ToPrettyString(args.User):target} ate {ToPrettyString(uid):food}");
            }

            _audio.Play(component.UseSound, Filter.Pvs(args.Target.Value), args.Target.Value, true, AudioParams.Default.WithVolume(-1f));

            // Try to break all used utensils
            foreach (var utensil in utensils)
            {
                _utensilSystem.TryBreak(utensil, args.User);
            }

            args.Repeat = !forceFeed;

            if (TryComp<StackComponent>(uid, out var stack))
            {
                //Not deleting whole stack piece will make troubles with grinding object
                if (stack.Count > 1)
                {
                    _stack.SetCount(uid, stack.Count - 1);
                    _solutionContainerSystem.TryAddSolution(uid, solution, split);
                    return;
                }
            }
            else if (component.UsesRemaining > 0)
            {
                return;
            }

            if (string.IsNullOrEmpty(component.TrashPrototype))
                EntityManager.QueueDeleteEntity(uid);

            else
                DeleteAndSpawnTrash(component, uid, args.User);
        }

        private void DeleteAndSpawnTrash(FoodComponent component, EntityUid food, EntityUid? user = null)
        {
            //We're empty. Become trash.
            var position = Transform(food).MapPosition;
            var finisher = EntityManager.SpawnEntity(component.TrashPrototype, position);

            // If the user is holding the item
            if (user != null && _handsSystem.IsHolding(user.Value, food, out var hand))
            {
                EntityManager.DeleteEntity(food);

                // Put the trash in the user's hand
                _handsSystem.TryPickup(user.Value, finisher, hand);
                return;
            }

            EntityManager.QueueDeleteEntity(food);
        }

        private void AddEatVerb(EntityUid uid, FoodComponent component, GetVerbsEvent<AlternativeVerb> ev)
        {
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
                    TryFeed(ev.User, ev.User, uid, component);
                },
                Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/cutlery.svg.192dpi.png")),
                Text = Loc.GetString("food-system-verb-eat"),
                Priority = -1
            };

            ev.Verbs.Add(verb);
        }

        /// <summary>
        ///     Returns true if the food item can be digested by the user.
        /// </summary>
        public bool IsDigestibleBy(EntityUid uid, EntityUid food, FoodComponent? foodComp = null)
        {
            if (!Resolve(food, ref foodComp, false))
                return false;

            if (!_bodySystem.TryGetBodyOrganComponents<StomachComponent>(uid, out var stomachs))
                return false;

            return IsDigestibleBy(food, foodComp, stomachs);
        }

        /// <summary>
        ///     Returns true if <paramref name="stomachs"/> has a <see cref="StomachComponent"/> that is capable of
        ///     digesting this <paramref name="food"/> (or if they even have enough stomachs in the first place).
        /// </summary>
        private bool IsDigestibleBy(EntityUid food, FoodComponent component, List<(StomachComponent, OrganComponent)> stomachs)
        {
            var digestible = true;

            if (stomachs.Count < component.RequiredStomachs)
                return false;

            if (!component.RequiresSpecialDigestion)
                return true;

            foreach (var (comp, _) in stomachs)
            {
                if (comp.SpecialDigestible == null)
                    continue;

                if (!comp.SpecialDigestible.IsValid(food, EntityManager))
                    return false;
            }

            return digestible;
        }

        private bool TryGetRequiredUtensils(EntityUid user, FoodComponent component,
            out List<EntityUid> utensils, HandsComponent? hands = null)
        {
            utensils = new List<EntityUid>();

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
                    utensils.Add(item);
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
