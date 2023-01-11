using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.DoAfter;
using Content.Server.Hands.Components;
using Content.Server.Hands.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.Alert;
using Content.Shared.Cuffs.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Database;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Popups;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Content.Server.Recycling.Components;

namespace Content.Server.Cuffs.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedCuffableComponent))]
    public sealed class CuffableComponent : SharedCuffableComponent
    {
        [Dependency] private readonly IEntityManager _entMan = default!;
        [Dependency] private readonly IEntitySystemManager _sysMan = default!;
        [Dependency] private readonly IComponentFactory _componentFactory = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;

        /// <summary>
        /// How many of this entity's hands are currently cuffed.
        /// </summary>
        [ViewVariables]
        public int CuffedHandCount => Container.ContainedEntities.Count * 2;

        public EntityUid LastAddedCuffs => Container.ContainedEntities[^1];

        public IReadOnlyList<EntityUid> StoredEntities => Container.ContainedEntities;

        private bool _uncuffing;

        protected override void Initialize()
        {
            base.Initialize();

            Owner.EnsureComponentWarn<HandsComponent>();
        }

        public override ComponentState GetComponentState()
        {
            // there are 2 approaches i can think of to handle the handcuff overlay on players
            // 1 - make the current RSI the handcuff type that's currently active. all handcuffs on the player will appear the same.
            // 2 - allow for several different player overlays for each different cuff type.
            // approach #2 would be more difficult/time consuming to do and the payoff doesn't make it worth it.
            // right now we're doing approach #1.

            if (CuffedHandCount > 0)
            {
                if (_entMan.TryGetComponent<HandcuffComponent?>(LastAddedCuffs, out var cuffs))
                {
                    return new CuffableComponentState(CuffedHandCount,
                       CanStillInteract,
                       cuffs.CuffedRSI,
                       $"{cuffs.OverlayIconState}-{CuffedHandCount}",
                       cuffs.Color);
                    // the iconstate is formatted as blah-2, blah-4, blah-6, etc.
                    // the number corresponds to how many hands are cuffed.
                }
            }

            return new CuffableComponentState(CuffedHandCount,
               CanStillInteract,
               "/Objects/Misc/handcuffs.rsi",
               "body-overlay-2",
               Color.White);
        }

        /// <summary>
        /// Add a set of cuffs to an existing CuffedComponent.
        /// </summary>
        /// <param name="prototype"></param>
        public bool TryAddNewCuffs(EntityUid user, EntityUid handcuff)
        {
            if (!_entMan.HasComponent<HandcuffComponent>(handcuff))
            {
                Logger.Warning($"Handcuffs being applied to player are missing a {nameof(HandcuffComponent)}!");
                return false;
            }

            if (!EntitySystem.Get<SharedInteractionSystem>().InRangeUnobstructed(handcuff, Owner))
            {
                Logger.Warning("Handcuffs being applied to player are obstructed or too far away! This should not happen!");
                return true;
            }

            var sys = _entMan.EntitySysManager.GetEntitySystem<SharedHandsSystem>();

            // Success!
            sys.TryDrop(user, handcuff);

            Container.Insert(handcuff);
            CanStillInteract = _entMan.TryGetComponent(Owner, out HandsComponent? ownerHands) && ownerHands.Hands.Count() > CuffedHandCount;
            _entMan.EntitySysManager.GetEntitySystem<ActionBlockerSystem>().UpdateCanMove(Owner);

            var ev = new CuffedStateChangeEvent();
            _entMan.EventBus.RaiseLocalEvent(Owner, ref ev, true);
            UpdateAlert();
            UpdateHeldItems(handcuff);
            Dirty(_entMan);
            return true;
        }

        public void CuffedStateChanged()
        {
            UpdateAlert();
            var ev = new CuffedStateChangeEvent();
            _entMan.EventBus.RaiseLocalEvent(Owner, ref ev, true);
        }

        /// <summary>
        ///     Adds virtual cuff items to the user's hands.
        /// </summary>
        public void UpdateHeldItems(EntityUid handcuff)
        {
            // TODO when ecs-ing this, we probably don't just want to use the generic virtual-item entity, and instead
            // want to add our own item, so that use-in-hand triggers an uncuff attempt and the like.

            if (!_entMan.TryGetComponent(Owner, out HandsComponent? handsComponent)) return;

            var handSys = _entMan.EntitySysManager.GetEntitySystem<SharedHandsSystem>();

            var freeHands = 0;
            foreach (var hand in handSys.EnumerateHands(Owner, handsComponent))
            {
                if (hand.HeldEntity == null)
                {
                    freeHands++;
                    continue;
                }

                // Is this entity removable? (it might be an existing handcuff blocker)
                if (_entMan.HasComponent<UnremoveableComponent>(hand.HeldEntity))
                    continue;

                handSys.DoDrop(Owner, hand, true, handsComponent);
                freeHands++;
                if (freeHands == 2)
                    break;
            }

            var virtSys = _entMan.EntitySysManager.GetEntitySystem<HandVirtualItemSystem>();

            if (virtSys.TrySpawnVirtualItemInHand(handcuff, Owner, out var virtItem1))
                _entMan.EnsureComponent<UnremoveableComponent>(virtItem1.Value);

            if (virtSys.TrySpawnVirtualItemInHand(handcuff, Owner, out var virtItem2))
                _entMan.EnsureComponent<UnremoveableComponent>(virtItem2.Value);
        }

        /// <summary>
        /// Updates the status effect indicator on the HUD.
        /// </summary>
        private void UpdateAlert()
        {
            if (CanStillInteract)
            {
                EntitySystem.Get<AlertsSystem>().ClearAlert(Owner, AlertType.Handcuffed);
            }
            else
            {
                EntitySystem.Get<AlertsSystem>().ShowAlert(Owner, AlertType.Handcuffed);
            }
        }

        /// <summary>
        /// Attempt to uncuff a cuffed entity. Can be called by the cuffed entity, or another entity trying to help uncuff them.
        /// If the uncuffing succeeds, the cuffs will drop on the floor.
        /// </summary>
        /// <param name="user">The cuffed entity</param>
        /// <param name="cuffsToRemove">Optional param for the handcuff entity to remove from the cuffed entity. If null, uses the most recently added handcuff entity.</param>
        public async void TryUncuff(EntityUid user, EntityUid? cuffsToRemove = null)
        {
            if (_uncuffing) return;

            var isOwner = user == Owner;

            if (cuffsToRemove == null)
            {
                if (Container.ContainedEntities.Count == 0)
                {
                    return;
                }

                cuffsToRemove = LastAddedCuffs;
            }
            else
            {
                if (!Container.ContainedEntities.Contains(cuffsToRemove.Value))
                {
                    Logger.Warning("A user is trying to remove handcuffs that aren't in the owner's container. This should never happen!");
                }
            }

            if (!_entMan.TryGetComponent<HandcuffComponent?>(cuffsToRemove, out var cuff))
            {
                Logger.Warning($"A user is trying to remove handcuffs without a {nameof(HandcuffComponent)}. This should never happen!");
                return;
            }

            var attempt = new UncuffAttemptEvent(user, Owner);
            _entMan.EventBus.RaiseLocalEvent(user, attempt, true);

            if (attempt.Cancelled)
            {
                return;
            }

            if (!isOwner && !EntitySystem.Get<SharedInteractionSystem>().InRangeUnobstructed(user, Owner))
            {
                user.PopupMessage(Loc.GetString("cuffable-component-cannot-remove-cuffs-too-far-message"));
                return;
            }

            user.PopupMessage(Loc.GetString("cuffable-component-start-removing-cuffs-message"));

            if (isOwner)
            {
                SoundSystem.Play(cuff.StartBreakoutSound.GetSound(), Filter.Pvs(Owner, entityManager: _entMan), Owner);
            }
            else
            {
                SoundSystem.Play(cuff.StartUncuffSound.GetSound(), Filter.Pvs(Owner, entityManager: _entMan), Owner);
            }

            var uncuffTime = isOwner ? cuff.BreakoutTime : cuff.UncuffTime;
            var doAfterEventArgs = new DoAfterEventArgs(user, uncuffTime, target: Owner)
            {
                BreakOnUserMove = true,
                BreakOnTargetMove = true,
                BreakOnDamage = true,
                BreakOnStun = true,
                NeedHand = true
            };

            var doAfterSystem = EntitySystem.Get<DoAfterSystem>();
            _uncuffing = true;

            var result = await doAfterSystem.WaitDoAfter(doAfterEventArgs);

            _uncuffing = false;

            if (result != DoAfterStatus.Cancelled)
            {
                Uncuff(user, cuffsToRemove.Value, cuff, isOwner);
            }
            else
            {
                user.PopupMessage(Loc.GetString("cuffable-component-remove-cuffs-fail-message"));
            }

            return;
        }

        //Lord forgive me for putting this here
        //Cuff ECS when
        public void Uncuff(EntityUid user, EntityUid cuffsToRemove, HandcuffComponent cuff, bool isOwner)
        {
            SoundSystem.Play(cuff.EndUncuffSound.GetSound(), Filter.Pvs(Owner), Owner);

            _entMan.EntitySysManager.GetEntitySystem<HandVirtualItemSystem>().DeleteInHandsMatching(user, cuffsToRemove);
            _entMan.EntitySysManager.GetEntitySystem<SharedHandsSystem>().PickupOrDrop(user, cuffsToRemove);

            if (cuff.BreakOnRemove)
            {
                cuff.Broken = true;

                    var meta = _entMan.GetComponent<MetaDataComponent>(cuffsToRemove);
                    meta.EntityName = Loc.GetString(cuff.BrokenName);
                    meta.EntityDescription = Loc.GetString(cuff.BrokenDesc);

                if (_entMan.TryGetComponent<SpriteComponent>(cuffsToRemove, out var sprite) && cuff.BrokenState != null)
                {
                    sprite.LayerSetState(0, cuff.BrokenState); // TODO: safety check to see if RSI contains the state?
                }

                _entMan.EnsureComponent<RecyclableComponent>(cuffsToRemove);
            }

            CanStillInteract = _entMan.TryGetComponent(Owner, out HandsComponent? handsComponent) && handsComponent.SortedHands.Count() > CuffedHandCount;
            _entMan.EntitySysManager.GetEntitySystem<ActionBlockerSystem>().UpdateCanMove(Owner);

            var ev = new CuffedStateChangeEvent();
            _entMan.EventBus.RaiseLocalEvent(Owner, ref ev, true);
            UpdateAlert();
            Dirty(_entMan);

            if (CuffedHandCount == 0)
            {
                user.PopupMessage(Loc.GetString("cuffable-component-remove-cuffs-success-message"));

                if (!isOwner)
                {
                    user.PopupMessage(Owner, Loc.GetString("cuffable-component-remove-cuffs-by-other-success-message", ("otherName", user)));
                }

                if (user == Owner)
                {
                    _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{_entMan.ToPrettyString(user):player} has successfully uncuffed themselves");
                }
                else
                {
                    _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{_entMan.ToPrettyString(user):player} has successfully uncuffed {_entMan.ToPrettyString(Owner):player}");
                }

            }
            else
            {
                if (!isOwner)
                {
                    user.PopupMessage(Loc.GetString("cuffable-component-remove-cuffs-partial-success-message", ("cuffedHandCount", CuffedHandCount), ("otherName", user)));
                    user.PopupMessage(Owner, Loc.GetString("cuffable-component-remove-cuffs-by-other-partial-success-message", ("otherName", user), ("cuffedHandCount", CuffedHandCount)));
                }
                else
                {
                    user.PopupMessage(Loc.GetString("cuffable-component-remove-cuffs-partial-success-message", ("cuffedHandCount", CuffedHandCount)));
                }
            }
        }
    }
}
