using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.CombatMode;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Input;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Physics;
using Content.Shared.Players.RateLimiting;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Tag;
using Content.Shared.Timing;
using Content.Shared.UserInterface;
using Content.Shared.Verbs;
using Content.Shared.Wall;
using JetBrains.Annotations;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Interaction
{
    /// <summary>
    /// Governs interactions during clicking on entities
    /// </summary>
    [UsedImplicitly]
    public abstract partial class SharedInteractionSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly INetManager _net = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] private readonly RotateToFaceSystem _rotateToFaceSystem = default!;
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        [Dependency] private readonly SharedPhysicsSystem _broadphase = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] private readonly SharedVerbSystem _verbSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly UseDelaySystem _useDelay = default!;
        [Dependency] private readonly PullingSystem _pullSystem = default!;
        [Dependency] private readonly InventorySystem _inventory = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly TagSystem _tagSystem = default!;
        [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
        [Dependency] private readonly SharedPlayerRateLimitManager _rateLimit = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly ISharedChatManager _chat = default!;

        private EntityQuery<IgnoreUIRangeComponent> _ignoreUiRangeQuery;
        private EntityQuery<FixturesComponent> _fixtureQuery;
        private EntityQuery<ItemComponent> _itemQuery;
        private EntityQuery<PhysicsComponent> _physicsQuery;
        private EntityQuery<HandsComponent> _handsQuery;
        private EntityQuery<InteractionRelayComponent> _relayQuery;
        private EntityQuery<CombatModeComponent> _combatQuery;
        private EntityQuery<WallMountComponent> _wallMountQuery;
        private EntityQuery<UseDelayComponent> _delayQuery;
        private EntityQuery<ActivatableUIComponent> _uiQuery;

        private const CollisionGroup InRangeUnobstructedMask = CollisionGroup.Impassable | CollisionGroup.InteractImpassable;

        public const float InteractionRange = 1.5f;
        public const float InteractionRangeSquared = InteractionRange * InteractionRange;
        public const float MaxRaycastRange = 100f;
        public const string RateLimitKey = "Interaction";

        public delegate bool Ignored(EntityUid entity);

        public override void Initialize()
        {
            _ignoreUiRangeQuery = GetEntityQuery<IgnoreUIRangeComponent>();
            _fixtureQuery = GetEntityQuery<FixturesComponent>();
            _itemQuery = GetEntityQuery<ItemComponent>();
            _physicsQuery = GetEntityQuery<PhysicsComponent>();
            _handsQuery = GetEntityQuery<HandsComponent>();
            _relayQuery = GetEntityQuery<InteractionRelayComponent>();
            _combatQuery = GetEntityQuery<CombatModeComponent>();
            _wallMountQuery = GetEntityQuery<WallMountComponent>();
            _delayQuery = GetEntityQuery<UseDelayComponent>();
            _uiQuery = GetEntityQuery<ActivatableUIComponent>();

            SubscribeLocalEvent<BoundUserInterfaceCheckRangeEvent>(HandleUserInterfaceRangeCheck);
            SubscribeLocalEvent<BoundUserInterfaceMessageAttempt>(OnBoundInterfaceInteractAttempt);

            SubscribeAllEvent<InteractInventorySlotEvent>(HandleInteractInventorySlotEvent);

            SubscribeLocalEvent<UnremoveableComponent, ContainerGettingRemovedAttemptEvent>(OnRemoveAttempt);
            SubscribeLocalEvent<UnremoveableComponent, GotUnequippedEvent>(OnUnequip);
            SubscribeLocalEvent<UnremoveableComponent, GotUnequippedHandEvent>(OnUnequipHand);
            SubscribeLocalEvent<UnremoveableComponent, DroppedEvent>(OnDropped);

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.AltActivateItemInWorld,
                    new PointerInputCmdHandler(HandleAltUseInteraction))
                .Bind(EngineKeyFunctions.Use,
                    new PointerInputCmdHandler(HandleUseInteraction))
                .Bind(ContentKeyFunctions.ActivateItemInWorld,
                    new PointerInputCmdHandler(HandleActivateItemInWorld))
                .Bind(ContentKeyFunctions.TryPullObject,
                    new PointerInputCmdHandler(HandleTryPullObject))
                .Register<SharedInteractionSystem>();

            _rateLimit.Register(RateLimitKey,
                new RateLimitRegistration(CCVars.InteractionRateLimitPeriod,
                    CCVars.InteractionRateLimitCount,
                    null,
                    CCVars.InteractionRateLimitAnnounceAdminsDelay,
                    RateLimitAlertAdmins)
            );

            InitializeBlocking();
        }

        private void RateLimitAlertAdmins(ICommonSession session)
        {
            _chat.SendAdminAlert(Loc.GetString("interaction-rate-limit-admin-announcement", ("player", session.Name)));
        }

        public override void Shutdown()
        {
            CommandBinds.Unregister<SharedInteractionSystem>();
            base.Shutdown();
        }

        /// <summary>
        ///     Check that the user that is interacting with the BUI is capable of interacting and can access the entity.
        /// </summary>
        private void OnBoundInterfaceInteractAttempt(BoundUserInterfaceMessageAttempt ev)
        {
            _uiQuery.TryComp(ev.Target, out var uiComp);
            if (!_actionBlockerSystem.CanInteract(ev.Actor, ev.Target))
            {
                // We permit ghosts to open uis unless explicitly blocked
                if (ev.Message is not OpenBoundInterfaceMessage || !HasComp<GhostComponent>(ev.Actor) || uiComp?.BlockSpectators == true)
                {
                    ev.Cancel();
                    return;
                }
            }

            var range = _ui.GetUiRange(ev.Target, ev.UiKey);

            // As long as range>0, the UI frame updates should have auto-closed the UI if it is out of range.
            DebugTools.Assert(range <= 0 || UiRangeCheck(ev.Actor, ev.Target, range));

            if (range <= 0 && !IsAccessible(ev.Actor, ev.Target))
            {
                ev.Cancel();
                return;
            }

            if (uiComp == null)
                return;

            if (uiComp.SingleUser && uiComp.CurrentSingleUser != null && uiComp.CurrentSingleUser != ev.Actor)
            {
                ev.Cancel();
                return;
            }

            if (uiComp.RequiresComplex && !_actionBlockerSystem.CanComplexInteract(ev.Actor))
                ev.Cancel();
        }

        private bool UiRangeCheck(Entity<TransformComponent?> user, Entity<TransformComponent?> target, float range)
        {
            if (!Resolve(target, ref target.Comp))
                return false;

            if (user.Owner == target.Owner)
                return true;

            // Fast check: if the user is the parent of the entity (e.g., holding it), we always assume that it is in range
            if (target.Comp.ParentUid == user.Owner)
                return true;

            return InRangeAndAccessible(user, target, range) || _ignoreUiRangeQuery.HasComp(user);
        }

        /// <summary>
        ///     Prevents an item with the Unremovable component from being removed from a container by almost any means
        /// </summary>
        private void OnRemoveAttempt(EntityUid uid, UnremoveableComponent item, ContainerGettingRemovedAttemptEvent args)
        {
            args.Cancel();
        }

        /// <summary>
        ///     If item has DeleteOnDrop true then item will be deleted if removed from inventory, if it is false then item
        ///     loses Unremoveable when removed from inventory (gibbing).
        /// </summary>
        private void OnUnequip(EntityUid uid, UnremoveableComponent item, GotUnequippedEvent args)
        {
            if (!item.DeleteOnDrop)
                RemCompDeferred<UnremoveableComponent>(uid);
            else if (_net.IsServer)
                QueueDel(uid);
        }

        private void OnUnequipHand(EntityUid uid, UnremoveableComponent item, GotUnequippedHandEvent args)
        {
            if (!item.DeleteOnDrop)
                RemCompDeferred<UnremoveableComponent>(uid);
            else if (_net.IsServer)
                QueueDel(uid);
        }

        private void OnDropped(EntityUid uid, UnremoveableComponent item, DroppedEvent args)
        {
            if (!item.DeleteOnDrop)
                RemCompDeferred<UnremoveableComponent>(uid);
            else if (_net.IsServer)
                QueueDel(uid);
        }

        private bool HandleTryPullObject(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
        {
            if (!ValidateClientInput(session, coords, uid, out var userEntity))
            {
                Log.Info($"TryPullObject input validation failed");
                return true;
            }

            //is this user trying to pull themself?
            if (userEntity.Value == uid)
                return false;

            if (Deleted(uid))
                return false;

            if (!InRangeUnobstructed(userEntity.Value, uid, popup: true))
                return false;

            _pullSystem.TogglePull(uid, userEntity.Value);
            return false;
        }

        /// <summary>
        ///     Handles the event were a client uses an item in their inventory or in their hands, either by
        ///     alt-clicking it or pressing 'E' while hovering over it.
        /// </summary>
        private void HandleInteractInventorySlotEvent(InteractInventorySlotEvent msg, EntitySessionEventArgs args)
        {
            var item = GetEntity(msg.ItemUid);

            // client sanitization
            if (!TryComp(item, out TransformComponent? itemXform) || !ValidateClientInput(args.SenderSession, itemXform.Coordinates, item, out var user))
            {
                Log.Info($"Inventory interaction validation failed.  Session={args.SenderSession}");
                return;
            }

            // We won't bother to check that the target item is ACTUALLY in an inventory slot. UserInteraction() and
            // InteractionActivate() should check that the item is accessible. So.. if a user wants to lie about an
            // in-reach item being used in a slot... that should have no impact. This is functionally the same as if
            // they had somehow directly clicked on that item.

            if (msg.AltInteract)
                // Use 'UserInteraction' function - behaves as if the user alt-clicked the item in the world.
                UserInteraction(user.Value, itemXform.Coordinates, item, msg.AltInteract);
            else
                // User used 'E'. We want to activate it, not simulate clicking on the item
                InteractionActivate(user.Value, item);
        }

        public bool HandleAltUseInteraction(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
        {
            // client sanitization
            if (!ValidateClientInput(session, coords, uid, out var user))
            {
                Log.Info($"Alt-use input validation failed");
                return true;
            }

            UserInteraction(user.Value, coords, uid, altInteract: true, checkAccess: ShouldCheckAccess(user.Value));

            return false;
        }

        public bool HandleUseInteraction(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
        {
            // client sanitization
            if (!ValidateClientInput(session, coords, uid, out var userEntity))
            {
                Log.Info($"Use input validation failed");
                return true;
            }

            UserInteraction(userEntity.Value, coords, !Deleted(uid) ? uid : null, checkAccess: ShouldCheckAccess(userEntity.Value));

            return false;
        }

        private bool ShouldCheckAccess(EntityUid user)
        {
            // This is for Admin/mapping convenience. If ever there are other ghosts that can still interact, this check
            // might need to be more selective.
            return !_tagSystem.HasTag(user, "BypassInteractionRangeChecks");
        }

        /// <summary>
        ///     Returns true if the specified entity should hand interact with the target instead of attacking
        /// </summary>
        /// <param name="user">The user interacting in combat mode</param>
        /// <param name="target">The target of the interaction</param>
        /// <returns></returns>
        public bool CombatModeCanHandInteract(EntityUid user, EntityUid? target)
        {
            // Always allow attack in these cases
            if (target == null || !_handsQuery.TryComp(user, out var hands) || hands.ActiveHand?.HeldEntity is not null)
                return false;

            // Only eat input if:
            // - Target isn't an item
            // - Target doesn't cancel should-interact event
            // This is intended to allow items to be picked up in combat mode,
            // but to also allow items to force attacks anyway (like mobs which are items, e.g. mice)
            if (!_itemQuery.HasComp(target))
                return false;

            var combatEv = new CombatModeShouldHandInteractEvent();
            RaiseLocalEvent(target.Value, ref combatEv);

            if (combatEv.Cancelled)
                return false;

            return true;
        }

        /// <summary>
        ///     Resolves user interactions with objects.
        /// </summary>
        /// <remarks>
        ///     Checks Whether combat mode is enabled and whether the user can actually interact with the given entity.
        /// </remarks>
        /// <param name="altInteract">Whether to use default or alternative interactions (usually as a result of
        /// alt+clicking). If combat mode is enabled, the alternative action is to perform the default non-combat
        /// interaction. Having an item in the active hand also disables alternative interactions.</param>
        public void UserInteraction(
            EntityUid user,
            EntityCoordinates coordinates,
            EntityUid? target,
            bool altInteract = false,
            bool checkCanInteract = true,
            bool checkAccess = true,
            bool checkCanUse = true)
        {
            if (_relayQuery.TryComp(user, out var relay) && relay.RelayEntity is not null)
            {
                // TODO this needs to be handled better. This probably bypasses many complex can-interact checks in weird roundabout ways.
                if (_actionBlockerSystem.CanInteract(user, target))
                {
                    UserInteraction(relay.RelayEntity.Value,
                        coordinates,
                        target,
                        altInteract,
                        checkCanInteract,
                        checkAccess,
                        checkCanUse);
                    return;
                }
            }

            if (target != null && Deleted(target.Value))
                return;

            if (!altInteract && _combatQuery.TryComp(user, out var combatMode) && combatMode.IsInCombatMode)
            {
                if (!CombatModeCanHandInteract(user, target))
                    return;
            }

            if (!ValidateInteractAndFace(user, coordinates))
                return;

            if (altInteract && target != null)
            {
                // Perform alternative interactions, using context menu verbs.
                // These perform their own range, can-interact, and accessibility checks.
                AltInteract(user, target.Value);
                return;
            }

            if (checkCanInteract && !_actionBlockerSystem.CanInteract(user, target))
                return;

            // Check if interacted entity is in the same container, the direct child, or direct parent of the user.
            // Also checks if the item is accessible via some storage UI (e.g., open backpack)
            if (checkAccess && target != null && !IsAccessible(user, target.Value))
                return;

            var inRangeUnobstructed = target == null
                ? !checkAccess || InRangeUnobstructed(user, coordinates)
                : !checkAccess || InRangeUnobstructed(user, target.Value); // permits interactions with wall mounted entities

            // empty-hand interactions
            // combat mode hand interactions will always be true here -- since
            // they check this earlier before returning in
            if (!TryGetUsedEntity(user, out var used, checkCanUse))
            {
                if (inRangeUnobstructed && target != null)
                    InteractHand(user, target.Value);

                return;
            }

            if (target == used)
            {
                UseInHandInteraction(user, target.Value, checkCanUse: false, checkCanInteract: false);
                return;
            }

            if (inRangeUnobstructed && target != null)
            {
                InteractUsing(
                    user,
                    used.Value,
                    target.Value,
                    coordinates,
                    checkCanInteract: false,
                    checkCanUse: false);

                return;
            }

            InteractUsingRanged(
                user,
                used.Value,
                target,
                coordinates,
                inRangeUnobstructed);
        }

        private bool IsDeleted(EntityUid uid)
        {
            return TerminatingOrDeleted(uid) || EntityManager.IsQueuedForDeletion(uid);
        }

        private bool IsDeleted(EntityUid? uid)
        {
            //optional / null entities can pass this validation check. I.e., is-deleted returns false for null uids
            return uid != null && IsDeleted(uid.Value);
        }

        public void InteractHand(EntityUid user, EntityUid target)
        {
            if (IsDeleted(user) || IsDeleted(target))
                return;

            var complexInteractions = _actionBlockerSystem.CanComplexInteract(user);
            if (!complexInteractions)
            {
                InteractionActivate(user,
                    target,
                    checkCanInteract: false,
                    checkUseDelay: true,
                    checkAccess: false,
                    complexInteractions: complexInteractions,
                    checkDeletion: false);
                return;
            }

            // allow for special logic before main interaction
            var ev = new BeforeInteractHandEvent(target);
            RaiseLocalEvent(user, ev);
            if (ev.Handled)
            {
                _adminLogger.Add(LogType.InteractHand, LogImpact.Low, $"{ToPrettyString(user):user} interacted with {ToPrettyString(target):target}, but it was handled by another system");
                return;
            }

            DebugTools.Assert(!IsDeleted(user) && !IsDeleted(target));
            // all interactions should only happen when in range / unobstructed, so no range check is needed
            var message = new InteractHandEvent(user, target);
            RaiseLocalEvent(target, message, true);
            _adminLogger.Add(LogType.InteractHand, LogImpact.Low, $"{ToPrettyString(user):user} interacted with {ToPrettyString(target):target}");
            DoContactInteraction(user, target, message);
            if (message.Handled)
                return;

            DebugTools.Assert(!IsDeleted(user) && !IsDeleted(target));
            // Else we run Activate.
            InteractionActivate(user,
                target,
                checkCanInteract: false,
                checkUseDelay: true,
                checkAccess: false,
                complexInteractions: complexInteractions,
                checkDeletion: false);
        }

        public void InteractUsingRanged(EntityUid user, EntityUid used, EntityUid? target,
            EntityCoordinates clickLocation, bool inRangeUnobstructed)
        {
            if (IsDeleted(user) || IsDeleted(used) || IsDeleted(target))
                return;

            if (target != null)
            {
                _adminLogger.Add(
                    LogType.InteractUsing,
                    LogImpact.Low,
                    $"{ToPrettyString(user):user} interacted with {ToPrettyString(target):target} using {ToPrettyString(used):used}");
            }
            else
            {
                _adminLogger.Add(
                    LogType.InteractUsing,
                    LogImpact.Low,
                    $"{ToPrettyString(user):user} interacted with *nothing* using {ToPrettyString(used):used}");
            }

            if (RangedInteractDoBefore(user, used, target, clickLocation, inRangeUnobstructed, checkDeletion: false))
                return;

            DebugTools.Assert(!IsDeleted(user) && !IsDeleted(used) && !IsDeleted(target));
            if (target != null)
            {
                var rangedMsg = new RangedInteractEvent(user, used, target.Value, clickLocation);
                RaiseLocalEvent(target.Value, rangedMsg, true);

                // We contact the USED entity, but not the target.
                DoContactInteraction(user, used, rangedMsg);
                if (rangedMsg.Handled)
                    return;
            }

            DebugTools.Assert(!IsDeleted(user) && !IsDeleted(used) && !IsDeleted(target));
            InteractDoAfter(user, used, target, clickLocation, inRangeUnobstructed, checkDeletion: false);
        }

        protected bool ValidateInteractAndFace(EntityUid user, EntityCoordinates coordinates)
        {
            // Verify user is on the same map as the entity they clicked on
            if (_transform.GetMapId(coordinates) != Transform(user).MapID)
                return false;

            if (!HasComp<NoRotateOnInteractComponent>(user))
                _rotateToFaceSystem.TryFaceCoordinates(user, _transform.ToMapCoordinates(coordinates).Position);

            return true;
        }

        /// <summary>
        ///     Traces a ray from coords to otherCoords and returns the length
        ///     of the vector between coords and the ray's first hit.
        /// </summary>
        /// <param name="origin">Set of coordinates to use.</param>
        /// <param name="other">Other set of coordinates to use.</param>
        /// <param name="collisionMask">the mask to check for collisions</param>
        /// <param name="predicate">
        ///     A predicate to check whether to ignore an entity or not.
        ///     If it returns true, it will be ignored.
        /// </param>
        /// <returns>Length of resulting ray.</returns>
        public float UnobstructedDistance(
            MapCoordinates origin,
            MapCoordinates other,
            int collisionMask = (int) InRangeUnobstructedMask,
            Ignored? predicate = null)
        {
            var dir = other.Position - origin.Position;

            if (dir.LengthSquared().Equals(0f))
                return 0f;

            predicate ??= _ => false;
            var ray = new CollisionRay(origin.Position, dir.Normalized(), collisionMask);
            var rayResults = _broadphase.IntersectRayWithPredicate(origin.MapId, ray, dir.Length(), predicate.Invoke, false).ToList();

            if (rayResults.Count == 0)
                return dir.Length();

            return (rayResults[0].HitPos - origin.Position).Length();
        }

        /// <summary>
        ///     Checks that these coordinates are within a certain distance without any
        ///     entity that matches the collision mask obstructing them.
        ///     If the <paramref name="range"/> is zero or negative,
        ///     this method will only check if nothing obstructs the two sets
        ///     of coordinates.
        /// </summary>
        /// <param name="origin">Set of coordinates to use.</param>
        /// <param name="other">Other set of coordinates to use.</param>
        /// <param name="range">
        ///     Maximum distance between the two sets of coordinates.
        /// </param>
        /// <param name="collisionMask">The mask to check for collisions.</param>
        /// <param name="predicate">
        ///     A predicate to check whether to ignore an entity or not.
        ///     If it returns true, it will be ignored.
        /// </param>
        /// <param name="checkAccess">Perform range checks</param>
        /// <returns>
        ///     True if the two points are within a given range without being obstructed.
        /// </returns>
        public bool InRangeUnobstructed(
            MapCoordinates origin,
            MapCoordinates other,
            float range = InteractionRange,
            CollisionGroup collisionMask = InRangeUnobstructedMask,
            Ignored? predicate = null,
            bool checkAccess = true)
        {
            // Have to be on same map regardless.
            if (other.MapId != origin.MapId)
                return false;

            if (!checkAccess)
                return true;

            var dir = other.Position - origin.Position;
            var length = dir.Length();

            // If range specified also check it
            if (range > 0f && length > range)
                return false;

            if (MathHelper.CloseTo(length, 0))
                return true;

            predicate ??= _ => false;

            if (length > MaxRaycastRange)
            {
                Log.Warning("InRangeUnobstructed check performed over extreme range. Limiting CollisionRay size.");
                length = MaxRaycastRange;
            }

            var ray = new CollisionRay(origin.Position, dir.Normalized(), (int) collisionMask);
            var rayResults = _broadphase.IntersectRayWithPredicate(origin.MapId, ray, length, predicate.Invoke, false).ToList();

            return rayResults.Count == 0;
        }

        public bool InRangeUnobstructed(
            Entity<TransformComponent?> origin,
            Entity<TransformComponent?> other,
            float range = InteractionRange,
            CollisionGroup collisionMask = InRangeUnobstructedMask,
            Ignored? predicate = null,
            bool popup = false)
        {
            if (!Resolve(other, ref other.Comp))
                return false;

            var ev = new InRangeOverrideEvent(origin, other);
            RaiseLocalEvent(origin, ref ev);

            if (ev.Handled)
            {
                return ev.InRange;
            }

            return InRangeUnobstructed(origin,
                other,
                other.Comp.Coordinates,
                other.Comp.LocalRotation,
                range,
                collisionMask,
                predicate,
                popup);
        }

        /// <summary>
        ///     Checks that two entities are within a certain distance without any
        ///     entity that matches the collision mask obstructing them.
        ///     If the <paramref name="range"/> is zero or negative,
        ///     this method will only check if nothing obstructs the two entities.
        ///     This function will also check whether the other entity is a wall-mounted entity. If it is, it will
        ///     automatically ignore some obstructions.
        /// </summary>
        /// <param name="origin">The first entity to use.</param>
        /// <param name="other">Other entity to use.</param>
        /// <param name="otherAngle">The local rotation to use for the other entity.</param>
        /// <param name="range">
        ///     Maximum distance between the two entities.
        /// </param>
        /// <param name="collisionMask">The mask to check for collisions.</param>
        /// <param name="predicate">
        ///     A predicate to check whether to ignore an entity or not.
        ///     If it returns true, it will be ignored.
        /// </param>
        /// <param name="popup">
        ///     Whether or not to popup a feedback message on the origin entity for
        ///     it to see.
        /// </param>
        /// <param name="otherCoordinates">The coordinates to use for the other entity.</param>
        /// <returns>
        ///     True if the two points are within a given range without being obstructed.
        /// </returns>
        public bool InRangeUnobstructed(
            Entity<TransformComponent?> origin,
            Entity<TransformComponent?> other,
            EntityCoordinates otherCoordinates,
            Angle otherAngle,
            float range = InteractionRange,
            CollisionGroup collisionMask = InRangeUnobstructedMask,
            Ignored? predicate = null,
            bool popup = false)
        {
            Ignored combinedPredicate = e => e == origin.Owner || (predicate?.Invoke(e) ?? false);
            var inRange = true;
            MapCoordinates originPos = default;
            var targetPos = _transform.ToMapCoordinates(otherCoordinates);
            Angle targetRot = default;

            // So essentially:
            // 1. If fixtures available check nearest point. We take in coordinates / angles because we might want to use a lag compensated position
            // 2. Fall back to centre of body.

            // Alternatively we could check centre distances first though
            // that means we wouldn't be able to easily check overlap interactions.
            if (range > 0f &&
                _fixtureQuery.TryComp(origin, out var fixtureA) &&
                // These fixture counts are stuff that has the component but no fixtures for <reasons> (e.g. buttons).
                // At least until they get removed.
                fixtureA.FixtureCount > 0 &&
                _fixtureQuery.TryComp(other, out var fixtureB) &&
                fixtureB.FixtureCount > 0 &&
                Resolve(origin, ref origin.Comp))
            {
                var (worldPosA, worldRotA) = origin.Comp.GetWorldPositionRotation();
                var xfA = new Transform(worldPosA, worldRotA);
                var parentRotB = _transform.GetWorldRotation(otherCoordinates.EntityId);
                var xfB = new Transform(targetPos.Position, parentRotB + otherAngle);

                // Different map or the likes.
                if (!_broadphase.TryGetNearest(
                        origin,
                        other,
                        out _,
                        out _,
                        out var distance,
                        xfA,
                        xfB,
                        fixtureA,
                        fixtureB))
                {
                    inRange = false;
                }
                // Overlap, early out and no raycast.
                else if (distance.Equals(0f))
                {
                    return true;
                }
                // Entity can bypass range checks.
                else if (!ShouldCheckAccess(origin))
                {
                    return true;
                }
                // Out of range so don't raycast.
                else if (distance > range)
                {
                    inRange = false;
                }
                else
                {
                    // We'll still do the raycast from the centres but we'll bump the range as we know they're in range.
                    originPos = _transform.GetMapCoordinates(origin, xform: origin.Comp);
                    range = (originPos.Position - targetPos.Position).Length();
                }
            }
            // No fixtures, e.g. wallmounts.
            else
            {
                originPos = _transform.GetMapCoordinates(origin, origin);
                var otherParent = (other.Comp ?? Transform(other)).ParentUid;
                targetRot = otherParent.IsValid() ? Transform(otherParent).LocalRotation + otherAngle : otherAngle;
            }

            // Do a raycast to check if relevant
            if (inRange)
            {
                var rayPredicate = GetPredicate(originPos, other, targetPos, targetRot, collisionMask, combinedPredicate);
                inRange = InRangeUnobstructed(originPos, targetPos, range, collisionMask, rayPredicate, ShouldCheckAccess(origin));
            }

            if (!inRange && popup && _gameTiming.IsFirstTimePredicted)
            {
                var message = Loc.GetString("interaction-system-user-interaction-cannot-reach");
                _popupSystem.PopupClient(message, origin, origin);
            }

            return inRange;
        }

        public bool InRangeUnobstructed(
            MapCoordinates origin,
            EntityUid target,
            float range = InteractionRange,
            CollisionGroup collisionMask = InRangeUnobstructedMask,
            Ignored? predicate = null)
        {
            var transform = Transform(target);
            var (position, rotation) = transform.GetWorldPositionRotation();
            var mapPos = new MapCoordinates(position, transform.MapID);
            var combinedPredicate = GetPredicate(origin, target, mapPos, rotation, collisionMask, predicate);

            return InRangeUnobstructed(origin, mapPos, range, collisionMask, combinedPredicate);
        }

        /// <summary>
        /// Gets the entities to ignore for an unobstructed raycast
        /// </summary>
        /// <example>
        /// if the target entity is a wallmount we ignore all other entities on the tile.
        /// </example>
        private Ignored GetPredicate(
            MapCoordinates origin,
            EntityUid target,
            MapCoordinates targetCoords,
            Angle targetRotation,
            CollisionGroup collisionMask,
            Ignored? predicate = null)
        {
            HashSet<EntityUid> ignored = new();

            if (_itemQuery.HasComp(target) && _physicsQuery.TryComp(target, out var physics) && physics.CanCollide)
            {
                // If the target is an item, we ignore any colliding entities. Currently done so that if items get stuck
                // inside of walls, users can still pick them up.
                ignored.UnionWith(_broadphase.GetEntitiesIntersectingBody(target, (int) collisionMask, false, physics));
            }
            else if (_wallMountQuery.TryComp(target, out var wallMount))
            {
                // wall-mount exemptions may be restricted to a specific angle range.da

                bool ignoreAnchored;
                if (wallMount.Arc >= Math.Tau)
                    ignoreAnchored = true;
                else
                {
                    var angle = Angle.FromWorldVec(origin.Position - targetCoords.Position);
                    var angleDelta = (wallMount.Direction + targetRotation - angle).Reduced().FlipPositive();
                    ignoreAnchored = angleDelta < wallMount.Arc / 2 || Math.Tau - angleDelta < wallMount.Arc / 2;
                }

                if (ignoreAnchored && _mapManager.TryFindGridAt(targetCoords, out _, out var grid))
                    ignored.UnionWith(grid.GetAnchoredEntities(targetCoords));
            }

            Ignored combinedPredicate = e => e == target || (predicate?.Invoke(e) ?? false) || ignored.Contains(e);
            return combinedPredicate;
        }

        /// <summary>
        ///     Checks that an entity and a set of grid coordinates are within a certain
        ///     distance without any entity that matches the collision mask
        ///     obstructing them.
        ///     If the <paramref name="range"/> is zero or negative,
        ///     this method will only check if nothing obstructs the entity and component.
        /// </summary>
        /// <param name="origin">The entity to use.</param>
        /// <param name="other">The grid coordinates to use.</param>
        /// <param name="range">
        ///     Maximum distance between the two entity and set of grid coordinates.
        /// </param>
        /// <param name="collisionMask">The mask to check for collisions.</param>
        /// <param name="predicate">
        ///     A predicate to check whether to ignore an entity or not.
        ///     If it returns true, it will be ignored.
        /// </param>
        /// <param name="popup">
        ///     Whether or not to popup a feedback message on the origin entity for
        ///     it to see.
        /// </param>
        /// <returns>
        ///     True if the two points are within a given range without being obstructed.
        /// </returns>
        public bool InRangeUnobstructed(
            EntityUid origin,
            EntityCoordinates other,
            float range = InteractionRange,
            CollisionGroup collisionMask = InRangeUnobstructedMask,
            Ignored? predicate = null,
            bool popup = false)
        {
            return InRangeUnobstructed(origin, _transform.ToMapCoordinates(other), range, collisionMask, predicate, popup);
        }

        /// <summary>
        ///     Checks that an entity and a set of map coordinates are within a certain
        ///     distance without any entity that matches the collision mask
        ///     obstructing them.
        ///     If the <paramref name="range"/> is zero or negative,
        ///     this method will only check if nothing obstructs the entity and component.
        /// </summary>
        /// <param name="origin">The entity to use.</param>
        /// <param name="other">The map coordinates to use.</param>
        /// <param name="range">
        ///     Maximum distance between the two entity and set of map coordinates.
        /// </param>
        /// <param name="collisionMask">The mask to check for collisions.</param>
        /// <param name="predicate">
        ///     A predicate to check whether to ignore an entity or not.
        ///     If it returns true, it will be ignored.
        /// </param>
        /// <param name="popup">
        ///     Whether or not to popup a feedback message on the origin entity for
        ///     it to see.
        /// </param>
        /// <returns>
        ///     True if the two points are within a given range without being obstructed.
        /// </returns>
        public bool InRangeUnobstructed(
            EntityUid origin,
            MapCoordinates other,
            float range = InteractionRange,
            CollisionGroup collisionMask = InRangeUnobstructedMask,
            Ignored? predicate = null,
            bool popup = false)
        {
            Ignored combinedPredicate = e => e == origin || (predicate?.Invoke(e) ?? false);
            var originPosition = _transform.GetMapCoordinates(origin);
            var inRange = InRangeUnobstructed(originPosition, other, range, collisionMask, combinedPredicate, ShouldCheckAccess(origin));

            if (!inRange && popup && _gameTiming.IsFirstTimePredicted)
            {
                var message = Loc.GetString("interaction-system-user-interaction-cannot-reach");
                _popupSystem.PopupEntity(message, origin, origin);
            }

            return inRange;
        }

        public bool RangedInteractDoBefore(
            EntityUid user,
            EntityUid used,
            EntityUid? target,
            EntityCoordinates clickLocation,
            bool canReach,
            bool checkDeletion = true)
        {
            if (checkDeletion && (IsDeleted(user) || IsDeleted(used) || IsDeleted(target)))
                return false;

            var ev = new BeforeRangedInteractEvent(user, used, target, clickLocation, canReach);
            RaiseLocalEvent(used, ev);

            if (!ev.Handled)
                return false;

            // We contact the USED entity, but not the target.
            DoContactInteraction(user, used, ev);
            return ev.Handled;
        }

        /// <summary>
        /// Uses an item/object on an entity
        /// Finds components with the InteractUsing interface and calls their function
        /// NOTE: Does not have an InRangeUnobstructed check
        /// </summary>
        /// <param name="user">User doing the interaction.</param>
        /// <param name="used">Item being used on the <paramref name="target"/>.</param>
        /// <param name="target">Entity getting interacted with by the <paramref name="user"/> using the
        ///     <paramref name="used"/> entity.</param>
        /// <param name="clickLocation">The location that the <paramref name="user"/> clicked.</param>
        /// <param name="checkCanInteract">Whether to check that the <paramref name="user"/> can interact with the
        ///     <paramref name="target"/>.</param>
        /// <param name="checkCanUse">Whether to check that the <paramref name="user"/> can use the
        ///     <paramref name="used"/> entity.</param>
        /// <returns>True if the interaction was handled. Otherwise, false.</returns>
        public bool InteractUsing(
            EntityUid user,
            EntityUid used,
            EntityUid target,
            EntityCoordinates clickLocation,
            bool checkCanInteract = true,
            bool checkCanUse = true)
        {
            if (IsDeleted(user) || IsDeleted(used) || IsDeleted(target))
                return false;

            if (checkCanInteract && !_actionBlockerSystem.CanInteract(user, target))
                return false;

            if (checkCanUse && !_actionBlockerSystem.CanUseHeldEntity(user, used))
                return false;

            _adminLogger.Add(
                LogType.InteractUsing,
                LogImpact.Low,
                $"{ToPrettyString(user):user} interacted with {ToPrettyString(target):target} using {ToPrettyString(used):used}");

            if (RangedInteractDoBefore(user, used, target, clickLocation, canReach: true, checkDeletion: false))
                return true;

            DebugTools.Assert(!IsDeleted(user) && !IsDeleted(used) && !IsDeleted(target));
            // all interactions should only happen when in range / unobstructed, so no range check is needed
            var interactUsingEvent = new InteractUsingEvent(user, used, target, clickLocation);
            RaiseLocalEvent(target, interactUsingEvent, true);
            DoContactInteraction(user, used, interactUsingEvent);
            DoContactInteraction(user, target, interactUsingEvent);
            // Contact interactions are currently only used for forensics, so we don't raise used -> target
            if (interactUsingEvent.Handled)
                return true;

            if (InteractDoAfter(user, used, target, clickLocation, canReach: true, checkDeletion: false))
                return true;

            DebugTools.Assert(!IsDeleted(user) && !IsDeleted(used) && !IsDeleted(target));
            return false;
        }

        /// <summary>
        ///     Used when clicking on an entity resulted in no other interaction. Used for low-priority interactions.
        /// </summary>
        /// <param name="user"><inheritdoc cref="InteractUsing"/></param>
        /// <param name="used"><inheritdoc cref="InteractUsing"/></param>
        /// <param name="target"><inheritdoc cref="InteractUsing"/></param>
        /// <param name="clickLocation"><inheritdoc cref="InteractUsing"/></param>
        /// <param name="canReach">Whether the <paramref name="user"/> is in range of the <paramref name="target"/>.
        ///     </param>
        /// <returns>True if the interaction was handled. Otherwise, false.</returns>
        public bool InteractDoAfter(EntityUid user, EntityUid used, EntityUid? target, EntityCoordinates clickLocation, bool canReach, bool checkDeletion = true)
        {
            if (target is { Valid: false })
                target = null;

            if (checkDeletion && (IsDeleted(user) || IsDeleted(used) || IsDeleted(target)))
                return false;

            var afterInteractEvent = new AfterInteractEvent(user, used, target, clickLocation, canReach);
            RaiseLocalEvent(used, afterInteractEvent);
            DoContactInteraction(user, used, afterInteractEvent);
            if (canReach)
            {
                DoContactInteraction(user, target, afterInteractEvent);
                // Contact interactions are currently only used for forensics, so we don't raise used -> target
            }

            if (afterInteractEvent.Handled)
                return true;

            if (target == null)
                return false;

            DebugTools.Assert(!IsDeleted(user) && !IsDeleted(used) && !IsDeleted(target));
            var afterInteractUsingEvent = new AfterInteractUsingEvent(user, used, target, clickLocation, canReach);
            RaiseLocalEvent(target.Value, afterInteractUsingEvent);

            DoContactInteraction(user, used, afterInteractUsingEvent);
            if (canReach)
            {
                DoContactInteraction(user, target, afterInteractUsingEvent);
                // Contact interactions are currently only used for forensics, so we don't raise used -> target
            }

            return afterInteractUsingEvent.Handled;
        }

        #region ActivateItemInWorld
        private bool HandleActivateItemInWorld(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
        {
            if (!ValidateClientInput(session, coords, uid, out var user))
            {
                Log.Info($"ActivateItemInWorld input validation failed");
                return false;
            }

            if (Deleted(uid))
                return false;

            InteractionActivate(user.Value, uid, checkAccess: ShouldCheckAccess(user.Value));
            return false;
        }

        /// <summary>
        /// Raises <see cref="ActivateInWorldEvent"/> events and activates the IActivate behavior of an object.
        /// </summary>
        /// <remarks>
        /// Does not check the can-use action blocker. In activations interacts can target entities outside of the users
        /// hands.
        /// </remarks>
        public bool InteractionActivate(
            EntityUid user,
            EntityUid used,
            bool checkCanInteract = true,
            bool checkUseDelay = true,
            bool checkAccess = true,
            bool? complexInteractions = null,
            bool checkDeletion = true)
        {
            if (checkDeletion && (IsDeleted(user) || IsDeleted(used)))
                return false;

            DebugTools.Assert(!IsDeleted(user) && !IsDeleted(used));
            _delayQuery.TryComp(used, out var delayComponent);
            if (checkUseDelay && delayComponent != null && _useDelay.IsDelayed((used, delayComponent)))
                return false;

            if (checkCanInteract && !_actionBlockerSystem.CanInteract(user, used))
                return false;

            if (checkAccess && !InRangeUnobstructed(user, used))
                return false;

            // Check if interacted entity is in the same container, the direct child, or direct parent of the user.
            // This is bypassed IF the interaction happened through an item slot (e.g., backpack UI)
            if (checkAccess && !IsAccessible(user, used))
                return false;

            complexInteractions ??= _actionBlockerSystem.CanComplexInteract(user);
            var activateMsg = new ActivateInWorldEvent(user, used, complexInteractions.Value);
            RaiseLocalEvent(used, activateMsg, true);
            if (activateMsg.Handled)
            {
                DoContactInteraction(user, used);
                if (!activateMsg.WasLogged)
                    _adminLogger.Add(LogType.InteractActivate, LogImpact.Low, $"{ToPrettyString(user):user} activated {ToPrettyString(used):used}");

                if (delayComponent != null)
                    _useDelay.TryResetDelay(used, component: delayComponent);
                return true;
            }

            DebugTools.Assert(!IsDeleted(user) && !IsDeleted(used));
            var userEv = new UserActivateInWorldEvent(user, used, complexInteractions.Value);
            RaiseLocalEvent(user, userEv, true);
            if (!userEv.Handled)
                return false;

            DoContactInteraction(user, used);
            // Still need to call this even without checkUseDelay in case this gets relayed from Activate.
            if (delayComponent != null)
                _useDelay.TryResetDelay(used, component: delayComponent);

            _adminLogger.Add(LogType.InteractActivate, LogImpact.Low, $"{ToPrettyString(user):user} activated {ToPrettyString(used):used}");
            return true;
        }
        #endregion

        #region Hands
        #region Use
        /// <summary>
        /// Raises UseInHandEvents and activates the IUse behaviors of an entity
        /// Does not check accessibility or range, for obvious reasons
        /// </summary>
        /// <returns>True if the interaction was handled. False otherwise</returns>
        public bool UseInHandInteraction(
            EntityUid user,
            EntityUid used,
            bool checkCanUse = true,
            bool checkCanInteract = true,
            bool checkUseDelay = true)
        {
            if (IsDeleted(user) || IsDeleted(used))
                return false;

            _delayQuery.TryComp(used, out var delayComponent);
            if (checkUseDelay && delayComponent != null && _useDelay.IsDelayed((used, delayComponent)))
                return true; // if the item is on cooldown, we consider this handled.

            if (checkCanInteract && !_actionBlockerSystem.CanInteract(user, used))
                return false;

            if (checkCanUse && !_actionBlockerSystem.CanUseHeldEntity(user, used))
                return false;

            var useMsg = new UseInHandEvent(user);
            RaiseLocalEvent(used, useMsg, true);
            if (useMsg.Handled)
            {
                DoContactInteraction(user, used, useMsg);
                if (delayComponent != null && useMsg.ApplyDelay)
                    _useDelay.TryResetDelay((used, delayComponent));
                return true;
            }

            DebugTools.Assert(!IsDeleted(user) && !IsDeleted(used));
            // else, default to activating the item
            return InteractionActivate(user, used, false, false, false, checkDeletion: false);
        }

        /// <summary>
        ///     Alternative interactions on an entity.
        /// </summary>
        /// <remarks>
        ///     Uses the context menu verb list, and acts out the highest priority alternative interaction verb.
        /// </remarks>
        /// <returns>True if the interaction was handled, false otherwise.</returns>
        public bool AltInteract(EntityUid user, EntityUid target)
        {
            // Get list of alt-interact verbs
            var verbs = _verbSystem.GetLocalVerbs(target, user, typeof(AlternativeVerb));

            if (verbs.Count == 0)
                return false;

            _verbSystem.ExecuteVerb(verbs.First(), user, target);
            return true;
        }
        #endregion

        public void DroppedInteraction(EntityUid user, EntityUid item)
        {
            if (IsDeleted(user) || IsDeleted(item))
                return;

            var dropMsg = new DroppedEvent(user);
            RaiseLocalEvent(item, dropMsg, true);

            // If the dropper is rotated then use their targetrelativerotation as the drop rotation
            var rotation = Angle.Zero;

            if (TryComp<InputMoverComponent>(user, out var mover))
            {
                rotation = mover.TargetRelativeRotation;
            }

            Transform(item).LocalRotation = rotation;
        }
        #endregion

        /// <summary>
        /// Check if a user can access a target (stored in the same containers) and is in range without obstructions.
        /// </summary>
        public bool InRangeAndAccessible(
            Entity<TransformComponent?> user,
            Entity<TransformComponent?> target,
            float range = InteractionRange,
            CollisionGroup collisionMask = InRangeUnobstructedMask,
            Ignored? predicate = null)
        {
            if (user == target)
                return true;

            if (!Resolve(user, ref user.Comp))
                return false;

            if (!Resolve(target, ref target.Comp))
                return false;

            return IsAccessible(user, target) && InRangeUnobstructed(user, target, range, collisionMask, predicate);
        }

        /// <summary>
        /// Check if a user can access a target or if they are stored in different containers.
        /// </summary>
        public bool IsAccessible(Entity<TransformComponent?> user, Entity<TransformComponent?> target)
        {
            var ev = new AccessibleOverrideEvent(user, target);

            RaiseLocalEvent(user, ref ev);

            if (ev.Handled)
                return ev.Accessible;

            if (_containerSystem.IsInSameOrParentContainer(user, target, out _, out var container))
                return true;

            return container != null && CanAccessViaStorage(user, target, container);
        }

        /// <summary>
        ///     If a target is in range, but not in the same container as the user, it may be inside of a backpack. This
        ///     checks if the user can access the item in these situations.
        /// </summary>
        public bool CanAccessViaStorage(EntityUid user, EntityUid target)
        {
            if (!_containerSystem.TryGetContainingContainer((target, null, null), out var container))
                return false;

            return CanAccessViaStorage(user, target, container);
        }

        /// <inheritdoc cref="CanAccessViaStorage(Robust.Shared.GameObjects.EntityUid,Robust.Shared.GameObjects.EntityUid)"/>
        public bool CanAccessViaStorage(EntityUid user, EntityUid target, BaseContainer container)
        {
            if (StorageComponent.ContainerId != container.ID)
                return false;

            // we don't check if the user can access the storage entity itself. This should be handed by the UI system.
            return _ui.IsUiOpen(container.Owner, StorageComponent.StorageUiKey.Key, user);
        }

        /// <summary>
        ///     Checks whether an entity currently equipped by another player is accessible to some user. This shouldn't
        ///     be used as a general interaction check, as these kinda of interactions should generally trigger a
        ///     do-after and a warning for the other player.
        /// </summary>
        public bool CanAccessEquipment(EntityUid user, EntityUid target)
        {
            if (Deleted(target))
                return false;

            if (!_containerSystem.TryGetContainingContainer((target, null, null), out var container))
                return false;

            var wearer = container.Owner;
            if (!_inventory.TryGetSlot(wearer, container.ID, out var slotDef))
                return false;

            if (wearer == user)
                return true;

            if (slotDef.StripHidden)
                return false;

            return InRangeUnobstructed(user, wearer) && _containerSystem.IsInSameOrParentContainer(user, wearer);
        }

        protected bool ValidateClientInput(
            ICommonSession? session,
            EntityCoordinates coords,
            EntityUid uid,
            [NotNullWhen(true)] out EntityUid? userEntity)
        {
            userEntity = null;

            if (!coords.IsValid(EntityManager))
            {
                Log.Info($"Invalid Coordinates: client={session}, coords={coords}");
                return false;
            }

            if (IsClientSide(uid))
            {
                Log.Warning($"Client sent interaction with client-side entity. Session={session}, Uid={uid}");
                return false;
            }

            userEntity = session?.AttachedEntity;

            if (userEntity == null || !userEntity.Value.Valid)
            {
                Log.Warning($"Client sent interaction with no attached entity. Session={session}");
                return false;
            }

            if (!Exists(userEntity))
            {
                Log.Warning($"Client attempted interaction with a non-existent attached entity. Session={session},  entity={userEntity}");
                return false;
            }

            return _rateLimit.CountAction(session!, RateLimitKey) == RateLimitStatus.Allowed;
        }

        /// <summary>
        ///     Simple convenience function to raise contact events (disease, forensics, etc).
        /// </summary>
        public void DoContactInteraction(EntityUid uidA, EntityUid? uidB, HandledEntityEventArgs? args = null)
        {
            if (uidB == null || args?.Handled == false)
                return;

            DebugTools.AssertNotEqual(uidA, uidB.Value);

            if (!TryComp(uidA, out MetaDataComponent? metaA) || metaA.EntityPaused)
                return;

            if (!TryComp(uidB, out MetaDataComponent? metaB) || metaB.EntityPaused)
                return ;

            // TODO Struct event
            var ev = new ContactInteractionEvent(uidB.Value);
            RaiseLocalEvent(uidA, ev);

            ev.Other = uidA;
            RaiseLocalEvent(uidB.Value, ev);
        }


        private void HandleUserInterfaceRangeCheck(ref BoundUserInterfaceCheckRangeEvent ev)
        {
            if (ev.Result == BoundUserInterfaceRangeResult.Fail)
                return;

            ev.Result = UiRangeCheck(ev.Actor!, ev.Target, ev.Data.InteractionRange)
                    ? BoundUserInterfaceRangeResult.Pass
                    : BoundUserInterfaceRangeResult.Fail;
        }

        /// <summary>
        /// Gets the entity that is currently being "used" for the interaction.
        /// In most cases, this refers to the entity in the character's active hand.
        /// </summary>
        /// <returns>If there is an entity being used.</returns>
        public bool TryGetUsedEntity(EntityUid user, [NotNullWhen(true)] out EntityUid? used, bool checkCanUse = true)
        {
            var ev = new GetUsedEntityEvent();
            RaiseLocalEvent(user, ref ev);

            used = ev.Used;
            if (!ev.Handled)
                return false;

            // Can the user use the held entity?
            if (checkCanUse && !_actionBlockerSystem.CanUseHeldEntity(user, ev.Used!.Value))
            {
                used = null;
                return false;
            }

            return ev.Handled;
        }

        [Obsolete("Use ActionBlockerSystem")]
        public bool SupportsComplexInteractions(EntityUid user)
        {
            return _actionBlockerSystem.CanComplexInteract(user);
        }
    }

    /// <summary>
    ///     Raised when a player attempts to activate an item in an inventory slot or hand slot
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class InteractInventorySlotEvent : EntityEventArgs
    {
        /// <summary>
        ///     Entity that was interacted with.
        /// </summary>
        public NetEntity ItemUid { get; }

        /// <summary>
        ///     Whether the interaction used the alt-modifier to trigger alternative interactions.
        /// </summary>
        public bool AltInteract { get; }

        public InteractInventorySlotEvent(NetEntity itemUid, bool altInteract = false)
        {
            ItemUid = itemUid;
            AltInteract = altInteract;
        }
    }

    /// <summary>
    ///     Raised directed by-ref on an entity to determine what item will be used in interactions.
    /// </summary>
    [ByRefEvent]
    public record struct GetUsedEntityEvent()
    {
        public EntityUid? Used = null;

        public bool Handled => Used != null;
    };

    /// <summary>
    ///     Raised directed by-ref on an item to determine if hand interactions should go through.
    ///     Defaults to allowing hand interactions to go through. Cancel to force the item to be attacked instead.
    /// </summary>
    /// <param name="Cancelled">Whether the hand interaction should be cancelled.</param>
    [ByRefEvent]
    public record struct CombatModeShouldHandInteractEvent(bool Cancelled = false);

    /// <summary>
    /// Override event raised directed on the user to say the target is accessible.
    /// </summary>
    /// <param name="User"></param>
    /// <param name="Target"></param>
    [ByRefEvent]
    public record struct AccessibleOverrideEvent(EntityUid User, EntityUid Target)
    {
        public readonly EntityUid User = User;
        public readonly EntityUid Target = Target;

        public bool Handled;
        public bool Accessible = false;
    }

    /// <summary>
    /// Override event raised directed on a user to check InRangeUnoccluded AND InRangeUnobstructed to the target if you require custom logic.
    /// </summary>
    [ByRefEvent]
    public record struct InRangeOverrideEvent(EntityUid User, EntityUid Target)
    {
        public readonly EntityUid User = User;
        public readonly EntityUid Target = Target;

        public bool Handled;
        public bool InRange = false;
    }
}
