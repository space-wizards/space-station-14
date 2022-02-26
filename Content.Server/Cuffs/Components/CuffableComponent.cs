using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.DoAfter;
using Content.Server.Hands.Components;
using Content.Shared.Alert;
using Content.Shared.Cuffs.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.ViewVariables;

namespace Content.Server.Cuffs.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedCuffableComponent))]
    public sealed class CuffableComponent : SharedCuffableComponent
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        /// <summary>
        /// How many of this entity's hands are currently cuffed.
        /// </summary>
        [ViewVariables]
        public int CuffedHandCount => Container.ContainedEntities.Count * 2;

        private EntityUid LastAddedCuffs => Container.ContainedEntities[^1];

        public IReadOnlyList<EntityUid> StoredEntities => Container.ContainedEntities;

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

            // Success!
            if (_entMan.TryGetComponent(user, out HandsComponent? handsComponent) && handsComponent.IsHolding(handcuff))
            {
                // Good lord handscomponent is scuffed, I hope some smug person will fix it someday
                handsComponent.Drop(handcuff);
            }

            Container.Insert(handcuff);
            CanStillInteract = _entMan.TryGetComponent(Owner, out HandsComponent? ownerHands) && ownerHands.HandNames.Count() > CuffedHandCount;

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
            if (!_entMan.TryGetComponent(Owner, out HandsComponent? handsComponent)) return;

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
            _entMan.EventBus.RaiseLocalEvent(user, attempt);

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

                Container.ForceRemove(cuffsToRemove.Value);
                var transform = _entMan.GetComponent<TransformComponent>(cuffsToRemove.Value);
                transform.AttachToGridOrMap();
                transform.WorldPosition = _entMan.GetComponent<TransformComponent>(Owner).WorldPosition;

                if (cuff.BreakOnRemove)
                {
                    cuff.Broken = true;

                    var meta = _entMan.GetComponent<MetaDataComponent>(cuffsToRemove.Value);
                    meta.EntityName = cuff.BrokenName;
                    meta.EntityDescription = cuff.BrokenDesc;

                    if (_entMan.TryGetComponent<SpriteComponent?>(cuffsToRemove, out var sprite) && cuff.BrokenState != null)
                    {
                        sprite.LayerSetState(0, cuff.BrokenState); // TODO: safety check to see if RSI contains the state?
                    }
                }

                CanStillInteract = _entMan.TryGetComponent(Owner, out HandsComponent? handsComponent) && handsComponent.HandNames.Count() > CuffedHandCount;
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
