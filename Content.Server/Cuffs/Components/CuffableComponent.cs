using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Alert;
using Content.Server.DoAfter;
using Content.Server.Hands.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Alert;
using Content.Shared.Cuffs.Components;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Players;
using Robust.Shared.ViewVariables;

namespace Content.Server.Cuffs.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedCuffableComponent))]
    public class CuffableComponent : SharedCuffableComponent
    {
        /// <summary>
        /// How many of this entity's hands are currently cuffed.
        /// </summary>
        [ViewVariables]
        public int CuffedHandCount => Container.ContainedEntities.Count * 2;

        protected IEntity LastAddedCuffs => Container.ContainedEntities[^1];

        public IReadOnlyList<IEntity> StoredEntities => Container.ContainedEntities;

        /// <summary>
        ///     Container of various handcuffs currently applied to the entity.
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        public Container Container { get; set; } = default!;

        // TODO: Make a component message
        public event Action? OnCuffedStateChanged;

        private bool _uncuffing;

        protected override void Initialize()
        {
            base.Initialize();

            Container = ContainerHelpers.EnsureContainer<Container>(Owner, Name);
            Owner.EnsureComponentWarn<HandsComponent>();
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            // there are 2 approaches i can think of to handle the handcuff overlay on players
            // 1 - make the current RSI the handcuff type that's currently active. all handcuffs on the player will appear the same.
            // 2 - allow for several different player overlays for each different cuff type.
            // approach #2 would be more difficult/time consuming to do and the payoff doesn't make it worth it.
            // right now we're doing approach #1.

            if (CuffedHandCount > 0)
            {
                if (LastAddedCuffs.TryGetComponent<HandcuffComponent>(out var cuffs))
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
        public bool TryAddNewCuffs(IEntity user, IEntity handcuff)
        {
            if (!handcuff.HasComponent<HandcuffComponent>())
            {
                Logger.Warning($"Handcuffs being applied to player are missing a {nameof(HandcuffComponent)}!");
                return false;
            }

            if (!handcuff.InRangeUnobstructed(Owner))
            {
                Logger.Warning("Handcuffs being applied to player are obstructed or too far away! This should not happen!");
                return true;
            }

            // Success!
            if (user.TryGetComponent(out HandsComponent? handsComponent) && handsComponent.IsHolding(handcuff))
            {
                // Good lord handscomponent is scuffed, I hope some smug person will fix it someday
                handsComponent.Drop(handcuff);
            }

            Container.Insert(handcuff);
            CanStillInteract = Owner.TryGetComponent(out HandsComponent? ownerHands) && ownerHands.HandNames.Count() > CuffedHandCount;

            OnCuffedStateChanged?.Invoke();
            UpdateAlert();
            UpdateHeldItems();
            Dirty();
            return true;
        }

        public void CuffedStateChanged()
        {
            UpdateAlert();
            OnCuffedStateChanged?.Invoke();
        }

        /// <summary>
        /// Check how many items the user is holding and if it's more than the number of cuffed hands, drop some items.
        /// </summary>
        public void UpdateHeldItems()
        {
            if (!Owner.TryGetComponent(out HandsComponent? handsComponent)) return;

            var itemCount = handsComponent.GetAllHeldItems().Count();
            var freeHandCount = handsComponent.HandNames.Count() - CuffedHandCount;

            if (freeHandCount < itemCount)
            {
                foreach (var item in handsComponent.GetAllHeldItems())
                {
                    if (freeHandCount < itemCount)
                    {
                        freeHandCount++;
                        handsComponent.Drop(item.Owner, false);
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Updates the status effect indicator on the HUD.
        /// </summary>
        private void UpdateAlert()
        {
            if (Owner.TryGetComponent(out ServerAlertsComponent? status))
            {
                if (CanStillInteract)
                {
                    status.ClearAlert(AlertType.Handcuffed);
                }
                else
                {
                    status.ShowAlert(AlertType.Handcuffed);
                }
            }
        }

        /// <summary>
        /// Attempt to uncuff a cuffed entity. Can be called by the cuffed entity, or another entity trying to help uncuff them.
        /// If the uncuffing succeeds, the cuffs will drop on the floor.
        /// </summary>
        /// <param name="user">The cuffed entity</param>
        /// <param name="cuffsToRemove">Optional param for the handcuff entity to remove from the cuffed entity. If null, uses the most recently added handcuff entity.</param>
        public async void TryUncuff(IEntity user, IEntity? cuffsToRemove = null)
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
                if (!Container.ContainedEntities.Contains(cuffsToRemove))
                {
                    Logger.Warning("A user is trying to remove handcuffs that aren't in the owner's container. This should never happen!");
                }
            }

            if (!cuffsToRemove.TryGetComponent<HandcuffComponent>(out var cuff))
            {
                Logger.Warning($"A user is trying to remove handcuffs without a {nameof(HandcuffComponent)}. This should never happen!");
                return;
            }

            var attempt = new UncuffAttemptEvent(user.Uid, Owner.Uid);
            Owner.EntityManager.EventBus.RaiseLocalEvent(user.Uid, attempt);

            if (attempt.Cancelled)
            {
                return;
            }

            if (!isOwner && !user.InRangeUnobstructed(Owner))
            {
                user.PopupMessage(Loc.GetString("cuffable-component-cannot-remove-cuffs-too-far-message"));
                return;
            }

            // TODO: Why are we even doing this check?
            if (!cuffsToRemove.InRangeUnobstructed(Owner))
            {
                Logger.Warning("Handcuffs being removed from player are obstructed or too far away! This should not happen!");
                return;
            }

            user.PopupMessage(Loc.GetString("cuffable-component-start-removing-cuffs-message"));

            if (isOwner)
            {
                SoundSystem.Play(Filter.Pvs(Owner), cuff.StartBreakoutSound.GetSound(), Owner);
            }
            else
            {
                SoundSystem.Play(Filter.Pvs(Owner), cuff.StartUncuffSound.GetSound(), Owner);
            }

            var uncuffTime = isOwner ? cuff.BreakoutTime : cuff.UncuffTime;
            var doAfterEventArgs = new DoAfterEventArgs(user, uncuffTime)
            {
                BreakOnUserMove = true,
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
                SoundSystem.Play(Filter.Pvs(Owner), cuff.EndUncuffSound.GetSound(), Owner);

                Container.ForceRemove(cuffsToRemove);
                cuffsToRemove.Transform.AttachToGridOrMap();
                cuffsToRemove.Transform.WorldPosition = Owner.Transform.WorldPosition;

                if (cuff.BreakOnRemove)
                {
                    cuff.Broken = true;

                    cuffsToRemove.Name = cuff.BrokenName;
                    cuffsToRemove.Description = cuff.BrokenDesc;

                    if (cuffsToRemove.TryGetComponent<SpriteComponent>(out var sprite) && cuff.BrokenState != null)
                    {
                        sprite.LayerSetState(0, cuff.BrokenState); // TODO: safety check to see if RSI contains the state?
                    }
                }

                CanStillInteract = Owner.TryGetComponent(out HandsComponent? handsComponent) && handsComponent.HandNames.Count() > CuffedHandCount;
                OnCuffedStateChanged?.Invoke();
                UpdateAlert();
                Dirty();

                if (CuffedHandCount == 0)
                {
                    user.PopupMessage(Loc.GetString("cuffable-component-remove-cuffs-success-message"));

                    if (!isOwner)
                    {
                        user.PopupMessage(Owner, Loc.GetString("cuffable-component-remove-cuffs-by-other-success-message", ("otherName", user)));
                    }
                }
                else
                {
                    if (!isOwner)
                    {
                        user.PopupMessage(Loc.GetString("cuffable-component-remove-cuffs-partial-success-message",
                                                        ("cuffedHandCount", CuffedHandCount),
                                                        ("otherName", user)));
                        user.PopupMessage(Owner, Loc.GetString("cuffable-component-remove-cuffs-by-other-partial-success-message",
                                                               ("otherName", user),
                                                               ("cuffedHandCount", CuffedHandCount)));
                    }
                    else
                    {
                        user.PopupMessage(Loc.GetString("cuffable-component-remove-cuffs-partial-success-message", ("cuffedHandCount", CuffedHandCount)));
                    }
                }
            }
            else
            {
                user.PopupMessage(Loc.GetString("cuffable-component-remove-cuffs-fail-message"));
            }

            return;
        }
    }
}
