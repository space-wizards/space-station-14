using System.Linq;
using Content.Server.Administration.Components;
using Content.Server.Administration.Logs;
using Content.Server.Hands.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Cuffs;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee.Events;
using JetBrains.Annotations;
using Content.Shared.Interaction;
using Robust.Shared.Containers;
using Content.Server.Hands.Systems;
using Content.Shared.Cuffs.Components;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Stunnable;
using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Server.Cuffs
{
    [UsedImplicitly]
    public sealed class CuffableSystem : SharedCuffableSystem
    {
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
        [Dependency] private readonly HandVirtualItemSystem _handVirtualItem = default!;
        [Dependency] private readonly MobStateSystem _mobState = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
        [Dependency] private readonly SharedHandsSystem _hands = default!;
        [Dependency] private readonly SharedInteractionSystem _interaction = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<UncuffAttemptEvent>(OnUncuffAttempt);
            SubscribeLocalEvent<CuffableComponent, GetVerbsEvent<Verb>>(AddUncuffVerb);
            SubscribeLocalEvent<HandcuffComponent, AfterInteractEvent>(OnCuffAfterInteract);
            SubscribeLocalEvent<HandcuffComponent, MeleeHitEvent>(OnCuffMeleeHit);
            SubscribeLocalEvent<CuffableComponent, EntRemovedFromContainerMessage>(OnCuffsRemoved);
            SubscribeLocalEvent<HandcuffComponent, ComponentGetState>(OnHandcuffGetState);
            SubscribeLocalEvent<CuffableComponent, ComponentGetState>(OnCuffableGetState);
            SubscribeLocalEvent<HandcuffComponent, DoAfterEvent>(OnHandcuffDoAfter);
            SubscribeLocalEvent<CuffableComponent, DoAfterEvent>(OnCuffableDoAfter);
        }

        private void OnCuffsRemoved(EntityUid uid, CuffableComponent component, EntRemovedFromContainerMessage args)
        {
            if (args.Container.ID == component.Container.ID)
                _handVirtualItem.DeleteInHandsMatching(uid, args.Entity);
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

        private void OnCuffAfterInteract(EntityUid uid, HandcuffComponent component, AfterInteractEvent args)
        {
            if (args.Target is not {Valid: true} target)
                return;

            if (!args.CanReach)
            {
                _popup.PopupEntity(Loc.GetString("handcuff-component-too-far-away-error"), args.User, args.User);
                return;
            }

            TryCuffing(uid, args.User, target, component);
            args.Handled = true;
        }

        public void TryCuffing(EntityUid handcuff, EntityUid user, EntityUid target, HandcuffComponent? handcuffComponent = null, CuffableComponent? cuffable = null)
        {
            if (!Resolve(handcuff, ref handcuffComponent) || !Resolve(target, ref cuffable, false))
                return;

            if (handcuffComponent.Cuffing)
                return;

            if (!TryComp<HandsComponent?>(target, out var hands))
            {
                _popup.PopupEntity(Loc.GetString("handcuff-component-target-has-no-hands-error",
                    ("targetName", Identity.Name(target, EntityManager, user))), user, user);
                return;
            }

            if (cuffable.CuffedHandCount >= hands.Count)
            {
                _popup.PopupEntity(Loc.GetString("handcuff-component-target-has-no-free-hands-error",
                    ("targetName", Identity.Name(target, EntityManager, user))), user, user);
                return;
            }

            // TODO these messages really need third-party variants. I.e., "{$user} starts cuffing {$target}!"
            if (target == user)
            {
                _popup.PopupEntity(Loc.GetString("handcuff-component-target-self"), user, user);
            }
            else
            {
                _popup.PopupEntity(Loc.GetString("handcuff-component-start-cuffing-target-message",
                    ("targetName", Identity.Name(target, EntityManager, user))), user, user);
                _popup.PopupEntity(Loc.GetString("handcuff-component-start-cuffing-by-other-message",
                    ("otherName", Identity.Name(user, EntityManager, target))), target, target);
            }

            _audio.PlayPvs(handcuffComponent.StartCuffSound, handcuff);


            var cuffTime = handcuffComponent.CuffTime;

            if (HasComp<StunnedComponent>(target))
            {
                cuffTime = MathF.Max(0.1f, cuffTime - handcuffComponent.StunBonus);
            }

            if (HasComp<DisarmProneComponent>(target))
                cuffTime = 0.0f; // cuff them instantly.

            var doAfterEventArgs = new DoAfterEventArgs(user, cuffTime, default, target, handcuff)
            {
                RaiseOnUsed = true,
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnDamage = true,
                BreakOnStun = true,
                NeedHand = true
            };

            handcuffComponent.Cuffing = true;
            _doAfter.DoAfter(doAfterEventArgs);
        }

        private void OnHandcuffDoAfter(EntityUid uid, HandcuffComponent component, DoAfterEvent args)
        {
            var user = args.Args.User;
            var target = args.Args.Target!.Value;
            component.Cuffing = false;

            if (!TryComp<CuffableComponent>(target, out var cuffable))
                return;

            // TODO these pop-ups need third-person variants (i.e. {$user} is cuffing {$target}!
            if (!args.Cancelled && TryAddNewCuffs(target, user, uid, cuffable))
            {
                _audio.PlayPredicted(component.EndCuffSound, uid, user);
                if (target == user)
                {
                    _popup.PopupEntity(Loc.GetString("handcuff-component-cuff-self-success-message"), user, user);
                    _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(user):player} has cuffed himself");
                }
                else
                {
                    _popup.PopupEntity(Loc.GetString("handcuff-component-cuff-other-success-message",
                        ("otherName", Identity.Name(target, EntityManager, user))), user, user);
                    _popup.PopupEntity(Loc.GetString("handcuff-component-cuff-by-other-success-message",
                        ("otherName", Identity.Name(user, EntityManager, target))), target, target);
                    _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(user):player} has cuffed {ToPrettyString(target):player}");
                }
            }
            else
            {
                if (target == user)
                {
                    _popup.PopupEntity(Loc.GetString("handcuff-component-cuff-interrupt-self-message"), user, user);
                }
                else
                {
                    _popup.PopupEntity(Loc.GetString("handcuff-component-cuff-interrupt-message",
                        ("targetName", Identity.Name(target, EntityManager, user))), user, user);
                    _popup.PopupEntity(Loc.GetString("handcuff-component-cuff-interrupt-other-message",
                        ("otherName", Identity.Name(user, EntityManager, target))), target, target);
                }
            }
        }

        private void OnCuffMeleeHit(EntityUid uid, HandcuffComponent component, MeleeHitEvent args)
        {
            if (!args.HitEntities.Any())
                return;

            TryCuffing(uid, args.User, args.HitEntities.First(), component);
            args.Handled = true;
        }

        private void OnUncuffAttempt(UncuffAttemptEvent args)
        {
            if (args.Cancelled)
            {
                return;
            }
            if (!EntityManager.EntityExists(args.User))
            {
                // Should this even be possible?
                args.Cancel();
                return;
            }
            // If the user is the target, special logic applies.
            // This is because the CanInteract blocking of the cuffs prevents self-uncuff.
            if (args.User == args.Target)
            {
                // This UncuffAttemptEvent check should probably be In MobStateSystem, not here?
                if (_mobState.IsIncapacitated(args.User))
                {
                    args.Cancel();
                }
                else
                {
                    // TODO Find a way for cuffable to check ActionBlockerSystem.CanInteract() without blocking itself
                }
            }
            else
            {
                // Check if the user can interact.
                if (!_actionBlocker.CanInteract(args.User, args.Target))
                {
                    args.Cancel();
                }
            }

            if (args.Cancelled)
            {
                _popup.PopupEntity(Loc.GetString("cuffable-component-cannot-interact-message"), args.Target, args.User);
            }
        }

        private void OnHandcuffGetState(EntityUid uid, HandcuffComponent component, ref ComponentGetState args)
        {
            args.State = new HandcuffComponentState(component.OverlayIconState);
        }

        private void OnCuffableGetState(EntityUid uid, CuffableComponent component, ref ComponentGetState args)
        {
            // there are 2 approaches i can think of to handle the handcuff overlay on players
            // 1 - make the current RSI the handcuff type that's currently active. all handcuffs on the player will appear the same.
            // 2 - allow for several different player overlays for each different cuff type.
            // approach #2 would be more difficult/time consuming to do and the payoff doesn't make it worth it.
            // right now we're doing approach #1.
            if (component.CuffedHandCount <= 0 || !TryComp<HandcuffComponent>(component.LastAddedCuffs, out var cuffs))
            {
                args.State = new CuffableComponentState(component.CuffedHandCount,
                    component.CanStillInteract,
                    "/Objects/Misc/handcuffs.rsi",
                    "body-overlay-2",
                    Color.White);
                return;
            }

            args.State = new CuffableComponentState(component.CuffedHandCount,
                component.CanStillInteract,
                cuffs.CuffedRSI,
                $"{cuffs.OverlayIconState}-{component.CuffedHandCount}",
                cuffs.Color);
            // the iconstate is formatted as blah-2, blah-4, blah-6, etc.
            // the number corresponds to how many hands are cuffed.
        }

        /// <summary>
        /// Add a set of cuffs to an existing CuffedComponent.
        /// </summary>
        public bool TryAddNewCuffs(EntityUid target, EntityUid user, EntityUid handcuff, CuffableComponent? component = null)
        {
            if (!Resolve(target, ref component))
                return false;

            if (!HasComp<HandcuffComponent>(handcuff))
            {
                Logger.Warning($"Handcuffs being applied to player are missing a {nameof(HandcuffComponent)}!");
                return false;
            }

            if (!_interaction.InRangeUnobstructed(handcuff, target))
            {
                Logger.Warning("Handcuffs being applied to player are obstructed or too far away! This should not happen!");
                return true;
            }

            // Success!
            _hands.TryDrop(user, handcuff);

            component.Container.Insert(handcuff);
            UpdateHeldItems(target, handcuff, component);
            return true;
        }

        /// <summary>
        ///     Adds virtual cuff items to the user's hands.
        /// </summary>
        private void UpdateHeldItems(EntityUid uid, EntityUid handcuff, CuffableComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            // TODO when ecs-ing this, we probably don't just want to use the generic virtual-item entity, and instead
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

            if (_handVirtualItem.TrySpawnVirtualItemInHand(handcuff, uid, out var virtItem1))
                EnsureComp<UnremoveableComponent>(virtItem1.Value);

            if (_handVirtualItem.TrySpawnVirtualItemInHand(handcuff, uid, out var virtItem2))
                EnsureComp<UnremoveableComponent>(virtItem2.Value);
        }

        /// <summary>
        /// Attempt to uncuff a cuffed entity. Can be called by the cuffed entity, or another entity trying to help uncuff them.
        /// If the uncuffing succeeds, the cuffs will drop on the floor.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="user">The cuffed entity</param>
        /// <param name="cuffsToRemove">Optional param for the handcuff entity to remove from the cuffed entity. If null, uses the most recently added handcuff entity.</param>
        /// <param name="cuffable"></param>
        public async void TryUncuff(EntityUid target, EntityUid user, EntityUid? cuffsToRemove = null, CuffableComponent? cuffable = null)
        {
            if (!Resolve(target, ref cuffable))
                return;

            if (cuffable.Uncuffing)
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
                    Logger.Warning("A user is trying to remove handcuffs that aren't in the owner's container. This should never happen!");
                }
            }

            if (!TryComp<HandcuffComponent?>(cuffsToRemove, out var cuff))
            {
                Logger.Warning($"A user is trying to remove handcuffs without a {nameof(HandcuffComponent)}. This should never happen!");
                return;
            }

            var attempt = new UncuffAttemptEvent(user, target);
            RaiseLocalEvent(user, attempt, true);

            if (attempt.Cancelled)
            {
                return;
            }

            if (!isOwner && !_interaction.InRangeUnobstructed(user, target))
            {
                _popup.PopupEntity(Loc.GetString("cuffable-component-cannot-remove-cuffs-too-far-message"), user, user);
                return;
            }

            _popup.PopupEntity(Loc.GetString("cuffable-component-start-removing-cuffs-message"), user, user);

            _audio.PlayPvs(isOwner ? cuff.StartBreakoutSound : cuff.StartUncuffSound, target);

            var uncuffTime = isOwner ? cuff.BreakoutTime : cuff.UncuffTime;
            var doAfterEventArgs = new DoAfterEventArgs(user, uncuffTime, default, target, cuffsToRemove)
            {
                RaiseOnTarget = true,
                BreakOnUserMove = true,
                BreakOnTargetMove = true,
                BreakOnDamage = true,
                BreakOnStun = true,
                NeedHand = true
            };

            cuffable.Uncuffing = true;
            _doAfter.DoAfter(doAfterEventArgs);
        }

        private void OnCuffableDoAfter(EntityUid uid, CuffableComponent component, DoAfterEvent args)
        {
            if (args.Args.Target is not { } target || args.Args.Used is not { } used)
                return;
            component.Uncuffing = false;

            var user = args.Args.User;

            if (args.Cancelled)
            {
                Uncuff(target, user, used, component);
            }
            else
            {
                _popup.PopupEntity(Loc.GetString("cuffable-component-remove-cuffs-fail-message"), user, user);
            }
        }

        public void Uncuff(EntityUid target, EntityUid user, EntityUid cuffsToRemove, CuffableComponent? cuffable = null, HandcuffComponent? cuff = null)
        {
            if (!Resolve(target, ref cuffable) || !Resolve(cuffsToRemove, ref cuff))
                return;

            _audio.PlayPvs(cuff.EndUncuffSound, target);

            cuffable.Container.Remove(cuffsToRemove);

            if (cuff.BreakOnRemove)
            {
                QueueDel(cuffsToRemove);
                var trash = Spawn(cuff.BrokenPrototype, MapCoordinates.Nullspace);
                _hands.PickupOrDrop(user, trash);
            }
            else
            {
                _hands.PickupOrDrop(user, cuffsToRemove);
            }

            if (cuffable.CuffedHandCount == 0)
            {
                _popup.PopupEntity(Loc.GetString("cuffable-component-remove-cuffs-success-message"), user, user);

                if (target != user)
                {
                    _popup.PopupEntity(Loc.GetString("cuffable-component-remove-cuffs-by-other-success-message",
                        ("otherName", Identity.Name(user, EntityManager, user))), target, user);
                    _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(user):player} has successfully uncuffed {ToPrettyString(target):player}");
                }
                else
                {
                    _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(user):player} has successfully uncuffed themselves");
                }
            }
            else
            {
                if (user != target)
                {
                    _popup.PopupEntity(Loc.GetString("cuffable-component-remove-cuffs-partial-success-message",
                        ("cuffedHandCount", cuffable.CuffedHandCount), ("otherName", Identity.Name(user, EntityManager, user))), user, user);
                    _popup.PopupEntity(Loc.GetString("cuffable-component-remove-cuffs-by-other-partial-success-message",
                        ("otherName", Identity.Name(user, EntityManager, user)), ("cuffedHandCount", cuffable.CuffedHandCount)), target, user);
                }
                else
                {
                    _popup.PopupEntity(Loc.GetString("cuffable-component-remove-cuffs-partial-success-message",
                        ("cuffedHandCount", cuffable.CuffedHandCount)), user, user);
                }
            }
        }
    }

    /// <summary>
    /// Event fired on the User when the User attempts to cuff the Target.
    /// Should generate popups on the User.
    /// </summary>
    public sealed class UncuffAttemptEvent : CancellableEntityEventArgs
    {
        public readonly EntityUid User;
        public readonly EntityUid Target;

        public UncuffAttemptEvent(EntityUid user, EntityUid target)
        {
            User = user;
            Target = target;
        }
    }
}
