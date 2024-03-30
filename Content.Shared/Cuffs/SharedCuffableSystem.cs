using System.Linq;
using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Alert;
using Content.Shared.Buckle.Components;
using Content.Shared.Cuffs.Components;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Item;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Popups;
using Content.Shared.Pulling.Events;
using Content.Shared.Rejuvenate;
using Content.Shared.Stunnable;
using Content.Shared.Timing;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using PullableComponent = Content.Shared.Movement.Pulling.Components.PullableComponent;

namespace Content.Shared.Cuffs
{
    // TODO remove all the IsServer() checks.
    public abstract partial class SharedCuffableSystem : EntitySystem
    {
        [Dependency] private readonly IComponentFactory _componentFactory = default!;
        [Dependency] private readonly INetManager _net = default!;
        [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
        [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
        [Dependency] private readonly AlertsSystem _alerts = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedContainerSystem _container = default!;
        [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
        [Dependency] private readonly SharedHandsSystem _hands = default!;
        [Dependency] private readonly SharedVirtualItemSystem _virtualItem = default!;
        [Dependency] private readonly SharedInteractionSystem _interaction = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] private readonly UseDelaySystem _delay = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<HandCountChangedEvent>(OnHandCountChanged);
            SubscribeLocalEvent<UncuffAttemptEvent>(OnUncuffAttempt);

            SubscribeLocalEvent<CuffableComponent, EntRemovedFromContainerMessage>(OnCuffsRemovedFromContainer);
            SubscribeLocalEvent<CuffableComponent, EntInsertedIntoContainerMessage>(OnCuffsInsertedIntoContainer);
            SubscribeLocalEvent<CuffableComponent, RejuvenateEvent>(OnRejuvenate);
            SubscribeLocalEvent<CuffableComponent, ComponentInit>(OnStartup);
            SubscribeLocalEvent<CuffableComponent, AttemptStopPullingEvent>(HandleStopPull);
            SubscribeLocalEvent<CuffableComponent, UpdateCanMoveEvent>(HandleMoveAttempt);
            SubscribeLocalEvent<CuffableComponent, IsEquippingAttemptEvent>(OnEquipAttempt);
            SubscribeLocalEvent<CuffableComponent, IsUnequippingAttemptEvent>(OnUnequipAttempt);
            SubscribeLocalEvent<CuffableComponent, BeingPulledAttemptEvent>(OnBeingPulledAttempt);
            SubscribeLocalEvent<CuffableComponent, BuckleAttemptEvent>(OnBuckleAttemptEvent);
            SubscribeLocalEvent<CuffableComponent, GetVerbsEvent<Verb>>(AddUncuffVerb);
            SubscribeLocalEvent<CuffableComponent, UnCuffDoAfterEvent>(OnCuffableDoAfter);
            SubscribeLocalEvent<CuffableComponent, PullStartedMessage>(OnPull);
            SubscribeLocalEvent<CuffableComponent, PullStoppedMessage>(OnPull);
            SubscribeLocalEvent<CuffableComponent, DropAttemptEvent>(CheckAct);
            SubscribeLocalEvent<CuffableComponent, PickupAttemptEvent>(CheckAct);
            SubscribeLocalEvent<CuffableComponent, AttackAttemptEvent>(CheckAct);
            SubscribeLocalEvent<CuffableComponent, UseAttemptEvent>(CheckAct);
            SubscribeLocalEvent<CuffableComponent, InteractionAttemptEvent>(CheckAct);

            SubscribeLocalEvent<HandcuffComponent, AfterInteractEvent>(OnCuffAfterInteract);
            SubscribeLocalEvent<HandcuffComponent, MeleeHitEvent>(OnCuffMeleeHit);
            SubscribeLocalEvent<HandcuffComponent, AddCuffDoAfterEvent>(OnAddCuffDoAfter);
            SubscribeLocalEvent<HandcuffComponent, VirtualItemDeletedEvent>(OnCuffVirtualItemDeleted);
        }

        private void OnUncuffAttempt(ref UncuffAttemptEvent args)
        {
            if (args.Cancelled)
                return;

            if (!Exists(args.User) || Deleted(args.User))
            {
                // Should this even be possible?
                args.Cancelled = true;
                return;
            }

            // If the user is the target, special logic applies.
            // This is because the CanInteract blocking of the cuffs prevents self-uncuff.
            if (args.User == args.Target)
            {
                if (!TryComp<CuffableComponent>(args.User, out var cuffable))
                {
                    DebugTools.Assert($"{args.User} tried to uncuff themselves but they are not cuffable.");
                    return;
                }

                // We temporarily allow interactions so the cuffable system does not block itself.
                // It's assumed that this will always be false.
                // Otherwise they would not be trying to uncuff themselves.
                cuffable.CanStillInteract = true;
                Dirty(args.User, cuffable);

                if (!_actionBlocker.CanInteract(args.User, args.User))
                    args.Cancelled = true;

                cuffable.CanStillInteract = false;
                Dirty(args.User, cuffable);
            }
            else
            {
                // Check if the user can interact.
                if (!_actionBlocker.CanInteract(args.User, args.Target))
                    args.Cancelled = true;
            }

            if (args.Cancelled)
            {
                _popup.PopupClient(Loc.GetString("cuffable-component-cannot-interact-message"), args.Target, args.User);
            }
        }

        private void OnStartup(EntityUid uid, CuffableComponent component, ComponentInit args)
        {
            component.Container = _container.EnsureContainer<Container>(uid, _componentFactory.GetComponentName(component.GetType()));
        }

        private void OnRejuvenate(EntityUid uid, CuffableComponent component, RejuvenateEvent args)
        {
            _container.EmptyContainer(component.Container, true);
        }

        private void OnCuffsRemovedFromContainer(EntityUid uid, CuffableComponent component, EntRemovedFromContainerMessage args)
        {
            // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
            if (args.Container.ID != component.Container?.ID)
                return;

            _virtualItem.DeleteInHandsMatching(uid, args.Entity);
            UpdateCuffState(uid, component);
        }

        private void OnCuffsInsertedIntoContainer(EntityUid uid, CuffableComponent component, ContainerModifiedMessage args)
        {
            if (args.Container == component.Container)
                UpdateCuffState(uid, component);
        }

        public void UpdateCuffState(EntityUid uid, CuffableComponent component)
        {
            var canInteract = TryComp(uid, out HandsComponent? hands) && hands.Hands.Count > component.CuffedHandCount;

            if (canInteract == component.CanStillInteract)
                return;

            component.CanStillInteract = canInteract;
            Dirty(uid, component);
            _actionBlocker.UpdateCanMove(uid);

            if (component.CanStillInteract)
                _alerts.ClearAlert(uid, AlertType.Handcuffed);
            else
                _alerts.ShowAlert(uid, AlertType.Handcuffed);

            var ev = new CuffedStateChangeEvent();
            RaiseLocalEvent(uid, ref ev);
        }

        private void OnBeingPulledAttempt(EntityUid uid, CuffableComponent component, BeingPulledAttemptEvent args)
        {
            if (!TryComp<PullableComponent>(uid, out var pullable))
                return;

            if (pullable.Puller != null && !component.CanStillInteract) // If we are being pulled already and cuffed, we can't get pulled again.
                args.Cancel();
        }

        private void OnBuckleAttemptEvent(EntityUid uid, CuffableComponent component, ref BuckleAttemptEvent args)
        {
            // if someone else is doing it, let it pass.
            if (args.UserEntity != uid)
                return;

            if (!TryComp<HandsComponent>(uid, out var hands) || component.CuffedHandCount != hands.Count)
                return;

            args.Cancelled = true;
            var message = args.Buckling
                ? Loc.GetString("handcuff-component-cuff-interrupt-buckled-message")
                : Loc.GetString("handcuff-component-cuff-interrupt-unbuckled-message");

            _popup.PopupClient(message, uid, args.UserEntity);
        }

        private void OnPull(EntityUid uid, CuffableComponent component, PullMessage args)
        {
            if (!component.CanStillInteract)
                _actionBlocker.UpdateCanMove(uid);
        }

        private void HandleMoveAttempt(EntityUid uid, CuffableComponent component, UpdateCanMoveEvent args)
        {
            if (component.CanStillInteract || !EntityManager.TryGetComponent(uid, out PullableComponent? pullable) || !pullable.BeingPulled)
                return;

            args.Cancel();
        }

        private void HandleStopPull(EntityUid uid, CuffableComponent component, AttemptStopPullingEvent args)
        {
            if (args.User == null || !Exists(args.User.Value))
                return;

            if (args.User.Value == uid && !component.CanStillInteract)
                args.Cancelled = true;
        }

        private void AddUncuffVerb(EntityUid uid, CuffableComponent component, GetVerbsEvent<Verb> args)
        {
            // Can the user access the cuffs, and is there even anything to uncuff?
            if (!args.CanAccess || component.CuffedHandCount == 0 || args.Hands == null)
                return;

            // We only check can interact if the user is not uncuffing themselves. As a result, the verb will show up
            // when the user is incapacitated & trying to uncuff themselves, but TryUncuff() will still fail when
            // attempted.
            if (args.User != args.Target && !args.CanInteract)
                return;

            Verb verb = new()
            {
                Act = () => TryUncuff(uid, args.User, cuffable: component),
                DoContactInteraction = true,
                Text = Loc.GetString("uncuff-verb-get-data-text")
            };
            //TODO VERB ICON add uncuffing symbol? may re-use the alert symbol showing that you are currently cuffed?
            args.Verbs.Add(verb);
        }

        private void OnCuffableDoAfter(EntityUid uid, CuffableComponent component, UnCuffDoAfterEvent args)
        {
            if (args.Args.Target is not { } target || args.Args.Used is not { } used)
                return;
            if (args.Handled)
                return;
            args.Handled = true;

            var user = args.Args.User;

            if (!args.Cancelled)
            {
                Uncuff(target, user, used, component);
            }
            else
            {
                _popup.PopupClient(Loc.GetString("cuffable-component-remove-cuffs-fail-message"), user, user);
            }
        }

        private void OnCuffAfterInteract(EntityUid uid, HandcuffComponent component, AfterInteractEvent args)
        {
            if (args.Target is not { Valid: true } target)
                return;

            if (!args.CanReach)
            {
                _popup.PopupClient(Loc.GetString("handcuff-component-too-far-away-error"), args.User, args.User);
                return;
            }

            var result = TryCuffing(args.User, target, uid, component);
            args.Handled = result;
        }

        private void OnCuffMeleeHit(EntityUid uid, HandcuffComponent component, MeleeHitEvent args)
        {
            if (!args.HitEntities.Any())
                return;

            TryCuffing(args.User, args.HitEntities.First(), uid, component);
            args.Handled = true;
        }

        private void OnAddCuffDoAfter(EntityUid uid, HandcuffComponent component, AddCuffDoAfterEvent args)
        {
            var user = args.Args.User;

            if (!TryComp<CuffableComponent>(args.Args.Target, out var cuffable))
                return;

            var target = args.Args.Target.Value;

            if (args.Handled)
                return;
            args.Handled = true;

            if (!args.Cancelled && TryAddNewCuffs(target, user, uid, cuffable))
            {
                component.Used = true;
                _audio.PlayPredicted(component.EndCuffSound, uid, user);

                _popup.PopupEntity(Loc.GetString("handcuff-component-cuff-observer-success-message",
                        ("user", Identity.Name(user, EntityManager)), ("target", Identity.Name(target, EntityManager))),
                    target, Filter.Pvs(target, entityManager: EntityManager)
                        .RemoveWhere(e => e.AttachedEntity == target || e.AttachedEntity == user), true);

                if (target == user)
                {
                    _popup.PopupClient(Loc.GetString("handcuff-component-cuff-self-success-message"), user, user);
                    _adminLog.Add(LogType.Action, LogImpact.Medium,
                        $"{ToPrettyString(user):player} has cuffed himself");
                }
                else
                {
                    _popup.PopupClient(Loc.GetString("handcuff-component-cuff-other-success-message",
                        ("otherName", Identity.Name(target, EntityManager, user))), user, user);
                    _popup.PopupClient(Loc.GetString("handcuff-component-cuff-by-other-success-message",
                        ("otherName", Identity.Name(user, EntityManager, target))), target, target);
                    _adminLog.Add(LogType.Action, LogImpact.Medium,
                        $"{ToPrettyString(user):player} has cuffed {ToPrettyString(target):player}");
                }
            }
            else
            {
                if (target == user)
                {
                    _popup.PopupClient(Loc.GetString("handcuff-component-cuff-interrupt-self-message"), user, user);
                }
                else
                {
                    // TODO Fix popup message wording
                    // This message assumes that the user being handcuffed is the one that caused the handcuff to fail.

                    _popup.PopupClient(Loc.GetString("handcuff-component-cuff-interrupt-message",
                        ("targetName", Identity.Name(target, EntityManager, user))), user, user);
                    _popup.PopupClient(Loc.GetString("handcuff-component-cuff-interrupt-other-message",
                        ("otherName", Identity.Name(user, EntityManager, target))), target, target);
                }
            }
        }

        private void OnCuffVirtualItemDeleted(EntityUid uid, HandcuffComponent component, VirtualItemDeletedEvent args)
        {
            Uncuff(args.User, null, uid, cuff: component);
        }

        /// <summary>
        ///     Check the current amount of hands the owner has, and if there's less hands than active cuffs we remove some cuffs.
        /// </summary>
        private void OnHandCountChanged(HandCountChangedEvent message)
        {
            var owner = message.Sender;

            if (!TryComp(owner, out CuffableComponent? cuffable) ||
                !cuffable.Initialized)
            {
                return;
            }

            var dirty = false;
            var handCount = CompOrNull<HandsComponent>(owner)?.Count ?? 0;

            while (cuffable.CuffedHandCount > handCount && cuffable.CuffedHandCount > 0)
            {
                dirty = true;

                var container = cuffable.Container;
                var entity = container.ContainedEntities[^1];

                _container.Remove(entity, container);
                _transform.SetWorldPosition(entity, _transform.GetWorldPosition(owner));
            }

            if (dirty)
            {
                UpdateCuffState(owner, cuffable);
            }
        }

        /// <summary>
        ///     Adds virtual cuff items to the user's hands.
        /// </summary>
        private void UpdateHeldItems(EntityUid uid, EntityUid handcuff, CuffableComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            // TODO we probably don't just want to use the generic virtual-item entity, and instead
            // want to add our own item, so that use-in-hand triggers an uncuff attempt and the like.

            if (!TryComp<HandsComponent>(uid, out var handsComponent))
                return;

            var freeHands = 0;
            foreach (var hand in _hands.EnumerateHands(uid, handsComponent))
            {
                if (hand.HeldEntity == null)
                {
                    freeHands++;
                    continue;
                }

                // Is this entity removable? (it might be an existing handcuff blocker)
                if (HasComp<UnremoveableComponent>(hand.HeldEntity))
                    continue;

                _hands.DoDrop(uid, hand, true, handsComponent);
                freeHands++;
                if (freeHands == 2)
                    break;
            }

            if (_virtualItem.TrySpawnVirtualItemInHand(handcuff, uid, out var virtItem1))
                EnsureComp<UnremoveableComponent>(virtItem1.Value);

            if (_virtualItem.TrySpawnVirtualItemInHand(handcuff, uid, out var virtItem2))
                EnsureComp<UnremoveableComponent>(virtItem2.Value);
        }

        /// <summary>
        /// Add a set of cuffs to an existing CuffedComponent.
        /// </summary>
        public bool TryAddNewCuffs(EntityUid target, EntityUid user, EntityUid handcuff, CuffableComponent? component = null, HandcuffComponent? cuff = null)
        {
            if (!Resolve(target, ref component) || !Resolve(handcuff, ref cuff))
                return false;

            if (!_interaction.InRangeUnobstructed(handcuff, target))
                return false;

            // Success!
            _hands.TryDrop(user, handcuff);

            _container.Insert(handcuff, component.Container);
            UpdateHeldItems(target, handcuff, component);
            return true;
        }

        /// <returns>False if the target entity isn't cuffable.</returns>
        public bool TryCuffing(EntityUid user, EntityUid target, EntityUid handcuff, HandcuffComponent? handcuffComponent = null, CuffableComponent? cuffable = null)
        {
            if (!Resolve(handcuff, ref handcuffComponent) || !Resolve(target, ref cuffable, false))
                return false;

            if (!TryComp<HandsComponent>(target, out var hands))
            {
                _popup.PopupClient(Loc.GetString("handcuff-component-target-has-no-hands-error",
                    ("targetName", Identity.Name(target, EntityManager, user))), user, user);
                return true;
            }

            if (cuffable.CuffedHandCount >= hands.Count)
            {
                _popup.PopupClient(Loc.GetString("handcuff-component-target-has-no-free-hands-error",
                    ("targetName", Identity.Name(target, EntityManager, user))), user, user);
                return true;
            }

            var cuffTime = handcuffComponent.CuffTime;

            if (HasComp<StunnedComponent>(target))
                cuffTime = MathF.Max(0.1f, cuffTime - handcuffComponent.StunBonus);

            if (HasComp<DisarmProneComponent>(target))
                cuffTime = 0.0f; // cuff them instantly.

            var doAfterEventArgs = new DoAfterArgs(EntityManager, user, cuffTime, new AddCuffDoAfterEvent(), handcuff, target, handcuff)
            {
                BreakOnMove = true,
                BreakOnWeightlessMove = false,
                BreakOnDamage = true,
                NeedHand = true,
                DistanceThreshold = 0.3f
            };

            if (!_doAfter.TryStartDoAfter(doAfterEventArgs))
                return true;

            _popup.PopupEntity(Loc.GetString("handcuff-component-start-cuffing-observer",
                    ("user", Identity.Name(user, EntityManager)), ("target", Identity.Name(target, EntityManager))),
                target, Filter.Pvs(target, entityManager: EntityManager)
                    .RemoveWhere(e => e.AttachedEntity == target || e.AttachedEntity == user), true);

            if (target == user)
            {
                _popup.PopupClient(Loc.GetString("handcuff-component-target-self"), user, user);
            }
            else
            {
                _popup.PopupClient(Loc.GetString("handcuff-component-start-cuffing-target-message",
                    ("targetName", Identity.Name(target, EntityManager, user))), user, user);
                _popup.PopupClient(Loc.GetString("handcuff-component-start-cuffing-by-other-message",
                    ("otherName", Identity.Name(user, EntityManager, target))), target, target);
            }

            _audio.PlayPredicted(handcuffComponent.StartCuffSound, handcuff, user);
            return true;
        }

        /// <summary>
        /// Attempt to uncuff a cuffed entity. Can be called by the cuffed entity, or another entity trying to help uncuff them.
        /// If the uncuffing succeeds, the cuffs will drop on the floor.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="user">The cuffed entity</param>
        /// <param name="cuffsToRemove">Optional param for the handcuff entity to remove from the cuffed entity. If null, uses the most recently added handcuff entity.</param>
        /// <param name="cuffable"></param>
        /// <param name="cuff"></param>
        public void TryUncuff(EntityUid target, EntityUid user, EntityUid? cuffsToRemove = null, CuffableComponent? cuffable = null, HandcuffComponent? cuff = null)
        {
            if (!Resolve(target, ref cuffable))
                return;

            var isOwner = user == target;

            if (cuffsToRemove == null)
            {
                if (cuffable.Container.ContainedEntities.Count == 0)
                {
                    return;
                }

                cuffsToRemove = cuffable.LastAddedCuffs;
            }
            else
            {
                if (!cuffable.Container.ContainedEntities.Contains(cuffsToRemove.Value))
                {
                    Log.Warning("A user is trying to remove handcuffs that aren't in the owner's container. This should never happen!");
                }
            }

            if (!Resolve(cuffsToRemove.Value, ref cuff))
                return;

            var attempt = new UncuffAttemptEvent(user, target);
            RaiseLocalEvent(user, ref attempt, true);

            if (attempt.Cancelled)
            {
                return;
            }

            if (!isOwner && !_interaction.InRangeUnobstructed(user, target))
            {
                _popup.PopupClient(Loc.GetString("cuffable-component-cannot-remove-cuffs-too-far-message"), user, user);
                return;
            }

            var uncuffTime = isOwner ? cuff.BreakoutTime : cuff.UncuffTime;

            if (isOwner)
            {
                if (!TryComp(cuffsToRemove.Value, out UseDelayComponent? useDelay))
                    return;

                if (!_delay.TryResetDelay((cuffsToRemove.Value, useDelay), true))
                {
                    return;
                }
            }

            var doAfterEventArgs = new DoAfterArgs(EntityManager, user, uncuffTime, new UnCuffDoAfterEvent(), target, target, cuffsToRemove)
            {
                BreakOnMove = true,
                BreakOnWeightlessMove = false,
                BreakOnDamage = true,
                NeedHand = true,
                RequireCanInteract = false, // Trust in UncuffAttemptEvent
                DistanceThreshold = 0.3f
            };

            if (!_doAfter.TryStartDoAfter(doAfterEventArgs))
                return;

            _adminLog.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(user)} is trying to uncuff {ToPrettyString(target)}");

            _popup.PopupEntity(Loc.GetString("cuffable-component-start-uncuffing-observer",
                    ("user", Identity.Name(user, EntityManager)), ("target", Identity.Name(target, EntityManager))),
                target, Filter.Pvs(target, entityManager: EntityManager)
                    .RemoveWhere(e => e.AttachedEntity == target || e.AttachedEntity == user), true);

            if (target == user)
            {
                _popup.PopupClient(Loc.GetString("cuffable-component-start-uncuffing-self"), user, user);
            }
            else
            {
                _popup.PopupClient(Loc.GetString("cuffable-component-start-uncuffing-target-message",
                    ("targetName", Identity.Name(target, EntityManager, user))), user, user);
                _popup.PopupClient(Loc.GetString("cuffable-component-start-uncuffing-by-other-message",
                    ("otherName", Identity.Name(user, EntityManager, target))), target, target);
            }

            _audio.PlayPredicted(isOwner ? cuff.StartBreakoutSound : cuff.StartUncuffSound, target, user);
        }

        public void Uncuff(EntityUid target, EntityUid? user, EntityUid cuffsToRemove, CuffableComponent? cuffable = null, HandcuffComponent? cuff = null)
        {
            if (!Resolve(target, ref cuffable) || !Resolve(cuffsToRemove, ref cuff))
                return;

            if (!cuff.Used || cuff.Removing || TerminatingOrDeleted(cuffsToRemove) || TerminatingOrDeleted(target))
                return;

            if (user != null)
            {
                var attempt = new UncuffAttemptEvent(user.Value, target);
                RaiseLocalEvent(user.Value, ref attempt);
                if (attempt.Cancelled)
                    return;
            }

            cuff.Removing = true;
            cuff.Used = false;
            _audio.PlayPredicted(cuff.EndUncuffSound, target, user);

            _container.Remove(cuffsToRemove, cuffable.Container);

            if (_net.IsServer)
            {
                // Handles spawning broken cuffs on server to avoid client misprediction
                if (cuff.BreakOnRemove)
                {
                    QueueDel(cuffsToRemove);
                    var trash = Spawn(cuff.BrokenPrototype, Transform(cuffsToRemove).Coordinates);
                    _hands.PickupOrDrop(user, trash);
                }
                else
                {
                    _hands.PickupOrDrop(user, cuffsToRemove);
                }
            }

            if (cuffable.CuffedHandCount == 0)
            {
                if (user != null)
                    _popup.PopupPredicted(Loc.GetString("cuffable-component-remove-cuffs-success-message"), user.Value, user.Value);

                if (target != user && user != null)
                {
                    _popup.PopupPredicted(Loc.GetString("cuffable-component-remove-cuffs-by-other-success-message",
                        ("otherName", Identity.Name(user.Value, EntityManager, user))), target, target);
                    _adminLog.Add(LogType.Action, LogImpact.Medium,
                        $"{ToPrettyString(user):player} has successfully uncuffed {ToPrettyString(target):player}");
                }
                else
                {
                    _adminLog.Add(LogType.Action, LogImpact.Medium,
                        $"{ToPrettyString(user):player} has successfully uncuffed themselves");
                }
            }
            else if (user != null)
            {
                if (user != target)
                {
                    _popup.PopupPredicted(Loc.GetString("cuffable-component-remove-cuffs-partial-success-message",
                        ("cuffedHandCount", cuffable.CuffedHandCount),
                        ("otherName", Identity.Name(user.Value, EntityManager, user.Value))), user.Value, user.Value);
                    _popup.PopupPredicted(Loc.GetString(
                        "cuffable-component-remove-cuffs-by-other-partial-success-message",
                        ("otherName", Identity.Name(user.Value, EntityManager, user.Value)),
                        ("cuffedHandCount", cuffable.CuffedHandCount)), target, target);
                }
                else
                {
                    _popup.PopupPredicted(Loc.GetString("cuffable-component-remove-cuffs-partial-success-message",
                        ("cuffedHandCount", cuffable.CuffedHandCount)), user.Value, user.Value);
                }
            }
            cuff.Removing = false;
        }

        #region ActionBlocker

        private void CheckAct(EntityUid uid, CuffableComponent component, CancellableEntityEventArgs args)
        {
            if (!component.CanStillInteract)
                args.Cancel();
        }

        private void OnEquipAttempt(EntityUid uid, CuffableComponent component, IsEquippingAttemptEvent args)
        {
            // is this a self-equip, or are they being stripped?
            if (args.Equipee == uid)
                CheckAct(uid, component, args);
        }

        private void OnUnequipAttempt(EntityUid uid, CuffableComponent component, IsUnequippingAttemptEvent args)
        {
            // is this a self-equip, or are they being stripped?
            if (args.Unequipee == uid)
                CheckAct(uid, component, args);
        }

        #endregion

        public IReadOnlyList<EntityUid> GetAllCuffs(CuffableComponent component)
        {
            return component.Container.ContainedEntities;
        }

        [Serializable, NetSerializable]
        private sealed partial class UnCuffDoAfterEvent : SimpleDoAfterEvent
        {
        }

        [Serializable, NetSerializable]
        private sealed partial class AddCuffDoAfterEvent : SimpleDoAfterEvent
        {
        }
    }
}
