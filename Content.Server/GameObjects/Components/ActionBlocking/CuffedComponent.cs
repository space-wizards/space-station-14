using Robust.Server.GameObjects;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Content.Server.GameObjects.EntitySystems.DoAfter;
using Robust.Shared.ViewVariables;
using Content.Server.GameObjects.Components.GUI;
using Content.Shared.GameObjects.Components.ActionBlocking;
using Content.Shared.GameObjects.Verbs;
using Content.Server.GameObjects.Components.Items.Storage;
using Robust.Shared.Log;
using System.Linq;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.EntitySystems;
using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Mobs;
using Robust.Shared.Maths;

namespace Content.Server.GameObjects.Components.ActionBlocking
{
    [RegisterComponent]
    public class CuffedComponent : SharedCuffedComponent
    {
#pragma warning disable 649
        [Dependency] private readonly ISharedNotifyManager _notifyManager;
#pragma warning restore 649

        /// <summary>
        /// How many of this entity's hands are currently cuffed.
        /// </summary>
        [ViewVariables]
        public int CuffedHandCount => _container.ContainedEntities.Count * 2;

        protected IEntity LastAddedCuffs => _container.ContainedEntities[_container.ContainedEntities.Count - 1];

        /// <summary>
        ///     Container of various handcuffs currently applied to the entity.
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        private Container _container = default!;

        private bool _deleteThisFrame = false;
        private bool _dirtyThisFrame = false;
        private float _interactRange;
        private DoAfterSystem _doAfterSystem;
        private AudioSystem _audioSystem;
        private HandsComponent _hands;

        public override void Initialize()
        {
            base.Initialize();

            _container = ContainerManagerComponent.Ensure<Container>(Name, Owner);
            _audioSystem = EntitySystem.Get<AudioSystem>();
            _doAfterSystem = EntitySystem.Get<DoAfterSystem>();
            _interactRange = SharedInteractionSystem.InteractionRange / 2;

            if (!Owner.TryGetComponent(out _hands))
            {
                Logger.Warning($"CuffedComponent was added to an entity that does not have a HandsComponent!");
                Owner.RemoveComponent<CuffedComponent>();
            }
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
                if (LastAddedCuffs.TryGetComponent<HandcuffComponent>(out var cuffs))
                {
                    return new CuffedComponentState(CuffedHandCount,
                       CanStillInteract,
                       cuffs.CuffedRSI,
                       $"{cuffs.OverlayIconState}-{CuffedHandCount}",
                       cuffs.Color);
                    // the iconstate is formatted as blah-2, blah-4, blah-6, etc.
                    // the number corresponds to how many hands are cuffed.
                }
            }

            return new CuffedComponentState(CuffedHandCount,
               CanStillInteract,
               "/Objects/Misc/handcuffs.rsi",
               "body-overlay-2",
               Color.White);
        }

        /// <summary>
        /// Add a set of cuffs to an existing CuffedComponent.
        /// </summary>
        /// <param name="prototype"></param>
        public void AddNewCuffs(IEntity handcuff)
        {
            _container.Insert(handcuff);
            CanStillInteract = _hands.Count > CuffedHandCount;

            UpdateStatusEffect();
            UpdateHeldItems();
            Dirty();
        }

        public void Update(float frameTime)
        {
            UpdateHandCount();
        }

        /// <summary>
        /// Check the current amount of hands the owner has, and if there's less hands than active cuffs we remove some cuffs.
        /// </summary>
        private void UpdateHandCount() 
        {
            _dirtyThisFrame = false;

            while (CuffedHandCount > _hands.Count && CuffedHandCount > 0)
            {
                _dirtyThisFrame = true;

                var entity = _container.ContainedEntities[_container.ContainedEntities.Count - 1];
                _container.Remove(entity);
                entity.Transform.WorldPosition = Owner.Transform.GridPosition.Position;
            }

            if (_dirtyThisFrame)
            {
                CanStillInteract = _hands.Count > CuffedHandCount;
                Dirty();
            }
        }

        /// <summary>
        /// Check how many items the user is holding and if it's more than the number of cuffed hands, drop some items.
        /// </summary>
        public void UpdateHeldItems()
        {
            var itemCount = _hands.GetAllHeldItems().Count();
            var freeHandCount = _hands.Count - CuffedHandCount;

            if (freeHandCount < itemCount)
            {
                foreach (ItemComponent item in _hands.GetAllHeldItems())
                {
                    if (freeHandCount < itemCount)
                    {
                        freeHandCount++;
                        _hands.Drop(item.Owner);
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
        private void UpdateStatusEffect()
        {
            if (Owner.TryGetComponent(out ServerStatusEffectsComponent status))
            {
                status.ChangeStatusEffectIcon(StatusEffect.Cuffed,
                    CanStillInteract ? "/Textures/Interface/StatusEffects/Handcuffed/Uncuffed.png" : "/Textures/Interface/StatusEffects/Handcuffed/Handcuffed.png");
            }
        }

        /// <summary>
        /// Attempt to uncuff a cuffed entity. Can be called by the cuffed entity, or another entity trying to help uncuff them.
        /// If the uncuffing succeeds, a prototype will be spawned on the floor.
        /// </summary>
        /// <param name="user">The cuffed entity</param>
        /// <param name="isOwner">Is the cuffed entity the owner of the CuffedComponent?</param>
        public async void TryUncuff(IEntity user, bool isOwner)
        {
            if (!LastAddedCuffs.TryGetComponent<HandcuffComponent>(out var cuff))
            {
                Logger.Warning($"A user is trying to remove handcuffs without a ${nameof(HandcuffComponent)}. This should never happen!");
                return;
            }

            if (!isOwner && !ActionBlockerSystem.CanInteract(user))
            {
                _notifyManager.PopupMessage(user, user, "You can't do that!");
                return;
            }

            if (!isOwner &&
                !EntitySystem.Get<SharedInteractionSystem>().InRangeUnobstructed(
                    user.Transform.MapPosition,
                    Owner.Transform.MapPosition,
                    _interactRange,
                    ignoredEnt: Owner))
            {
                _notifyManager.PopupMessage(user, user, "You are too far away to remove the cuffs.");
                return;
            }

            _notifyManager.PopupMessage(user, user, "You start removing the cuffs.");
            _audioSystem.PlayFromEntity(isOwner ? cuff.StartBreakoutSound : cuff.StartUncuffSound, Owner);

            var uncuffTime = isOwner ? cuff.BreakoutTime : cuff.UncuffTime;
            var doAfterEventArgs = new DoAfterEventArgs(user, uncuffTime)
            {
                BreakOnUserMove = true,
                BreakOnDamage = true,
                BreakOnStun = true,
                NeedHand = true
            };

            var result = await _doAfterSystem.DoAfter(doAfterEventArgs);

            if (result != DoAfterStatus.Cancelled)
            {
                _audioSystem.PlayFromEntity(cuff.EndUncuffSound, Owner);

                var cuffEntity = LastAddedCuffs;
                _container.ForceRemove(cuffEntity);
                cuffEntity.Transform.AttachToGridOrMap();
                cuffEntity.Transform.WorldPosition = Owner.Transform.WorldPosition;

                if (cuff.BreakOnRemove)
                {
                    cuff.Broken = true;

                    cuffEntity.Name = cuff.BrokenName;
                    cuffEntity.Description = cuff.BrokenDesc;

                    if (cuffEntity.TryGetComponent<SpriteComponent>(out var sprite))
                    {
                        sprite.LayerSetState(0, cuff.BrokenState); // TODO: safety check to see if RSI contains the state?
                    }
                }

                CanStillInteract = _hands.Count > CuffedHandCount;
                UpdateStatusEffect();
                Dirty();

                if (CuffedHandCount == 0)
                {
                    _notifyManager.PopupMessage(user, user, "You successfully remove the cuffs.");

                    if (!isOwner)
                    {
                        _notifyManager.PopupMessage(user, Owner, $"{user.Name} uncuffs your hands.");
                    }

                    _deleteThisFrame = true; // need a better way to do this.
                }
                else
                {
                    if (!isOwner)
                    {
                        _notifyManager.PopupMessage(user, user, $"You successfully remove the cuffs. {CuffedHandCount} of {user.Name}'s hands remain cuffed.");
                        _notifyManager.PopupMessage(user, Owner, $"{user.Name} removes your cuffs. {CuffedHandCount} of your hands remain cuffed.");
                    }
                    else
                    {
                        _notifyManager.PopupMessage(user, user, $"You successfully remove the cuffs. {CuffedHandCount} of your hands remain cuffed.");
                    }
                }
            }
            else
            {
                _notifyManager.PopupMessage(user, user, "You fail to remove the cuffs.");
            }

            return;
        }

        /// <summary>
        /// Allows the uncuffing of a cuffed person. Used by other people and by the component owner to break out of cuffs.
        /// </summary>
        [Verb]
        private sealed class UncuffVerb : Verb<CuffedComponent>
        {
            protected override void GetData(IEntity user, CuffedComponent component, VerbData data)
            {
                if ((user != component.Owner && !ActionBlockerSystem.CanInteract(user)) || component.CuffedHandCount == 0)
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("Uncuff");
            }

            protected override void Activate(IEntity user, CuffedComponent component)
            {
                if (component.CuffedHandCount > 0)
                {
                    component.TryUncuff(user, isOwner: user.Uid == component.Owner.Uid);
                }
            }
        }
    }
}
