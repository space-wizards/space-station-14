using System.Linq;
using Content.Server.Cuffs.Components;
using Content.Server.Hands.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Cuffs;
using Content.Shared.Hands;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee.Events;
using JetBrains.Annotations;
using Robust.Shared.Player;
using Content.Shared.Interaction;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Content.Server.Hands.Systems;
using Content.Shared.MobState.EntitySystems;

namespace Content.Server.Cuffs
{
    [UsedImplicitly]
    public sealed class CuffableSystem : SharedCuffableSystem
    {
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] private readonly HandVirtualItemSystem _virtualSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedMobStateSystem _mobState = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<HandCountChangedEvent>(OnHandCountChanged);
            SubscribeLocalEvent<UncuffAttemptEvent>(OnUncuffAttempt);
            SubscribeLocalEvent<CuffableComponent, GetVerbsEvent<Verb>>(AddUncuffVerb);
            SubscribeLocalEvent<HandcuffComponent, AfterInteractEvent>(OnCuffAfterInteract);
            SubscribeLocalEvent<HandcuffComponent, MeleeHitEvent>(OnCuffMeleeHit);
            SubscribeLocalEvent<CuffableComponent, EntRemovedFromContainerMessage>(OnCuffsRemoved);
        }

        private void OnCuffsRemoved(EntityUid uid, CuffableComponent component, EntRemovedFromContainerMessage args)
        {
            if (args.Container.ID == component.Container.ID)
                _virtualSystem.DeleteInHandsMatching(uid, args.Entity);
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
                Act = () => component.TryUncuff(args.User),
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

            TryCuffing(uid, args.User, args.Target.Value, component);
            args.Handled = true;
        }

        private void TryCuffing(EntityUid handcuff, EntityUid user, EntityUid target, HandcuffComponent component)
        {
            if (component.Cuffing || !EntityManager.TryGetComponent<CuffableComponent>(target, out var cuffed))
                return;

            if (component.Broken)
            {
                _popup.PopupEntity(Loc.GetString("handcuff-component-cuffs-broken-error"), user, user);
                return;
            }

            if (!EntityManager.TryGetComponent<HandsComponent?>(target, out var hands))
            {
                _popup.PopupEntity(Loc.GetString("handcuff-component-target-has-no-hands-error",("targetName", target)), user, user);
                return;
            }

            if (cuffed.CuffedHandCount >= hands.Count)
            {
                _popup.PopupEntity(Loc.GetString("handcuff-component-target-has-no-free-hands-error",("targetName", target)), user, user);
                return;
            }

            // TODO these messages really need third-party variants. I.e., "{$user} starts cuffing {$target}!"
            if (target == user)
            {
                _popup.PopupEntity(Loc.GetString("handcuff-component-target-self"), user, user);
            }
            else
            {
                _popup.PopupEntity(Loc.GetString("handcuff-component-start-cuffing-target-message",("targetName", target)), user, user);
                _popup.PopupEntity(Loc.GetString("handcuff-component-start-cuffing-by-other-message",("otherName", user)), target, target);
            }

            _audio.PlayPvs(component.StartCuffSound, handcuff);

            component.TryUpdateCuff(user, target, cuffed);
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
                if (!_actionBlockerSystem.CanInteract(args.User, args.Target))
                {
                    args.Cancel();
                }
            }
            if (args.Cancelled)
            {
                _popup.PopupEntity(Loc.GetString("cuffable-component-cannot-interact-message"), args.Target, args.User);
            }
        }

        /// <summary>
        ///     Check the current amount of hands the owner has, and if there's less hands than active cuffs we remove some cuffs.
        /// </summary>
        private void OnHandCountChanged(HandCountChangedEvent message)
        {
            var owner = message.Sender;

            if (!EntityManager.TryGetComponent(owner, out CuffableComponent? cuffable) ||
                !cuffable.Initialized)
            {
                return;
            }

            var dirty = false;
            var handCount = EntityManager.GetComponentOrNull<HandsComponent>(owner)?.Count ?? 0;

            while (cuffable.CuffedHandCount > handCount && cuffable.CuffedHandCount > 0)
            {
                dirty = true;

                var container = cuffable.Container;
                var entity = container.ContainedEntities[^1];

                container.Remove(entity);
                EntityManager.GetComponent<TransformComponent>(entity).WorldPosition = EntityManager.GetComponent<TransformComponent>(owner).WorldPosition;
            }

            if (dirty)
            {
                cuffable.CanStillInteract = handCount > cuffable.CuffedHandCount;
                _actionBlockerSystem.UpdateCanMove(cuffable.Owner);
                cuffable.CuffedStateChanged();
                Dirty(cuffable);
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
