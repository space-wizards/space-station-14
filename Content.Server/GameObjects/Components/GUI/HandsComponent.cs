#nullable enable
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Pulling;
using Content.Server.GameObjects.EntitySystems.Click;
using Content.Server.Interfaces.GameObjects;
using Content.Server.Interfaces.GameObjects.Components.Items;
using Content.Server.Utility;
using Content.Shared.Audio;
using Content.Shared.GameObjects.Components.Body.Part;
using Content.Shared.GameObjects.Components.Items;
using Content.Shared.GameObjects.Components.Pulling;
using Content.Shared.Interfaces;
using Content.Shared.Physics.Pull;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Players;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Server.GameObjects.Components.GUI
{
    [RegisterComponent]
    [ComponentReference(typeof(IHandsComponent))]
    [ComponentReference(typeof(ISharedHandsComponent))]
    [ComponentReference(typeof(SharedHandsComponent))]
    public class HandsComponent : SharedHandsComponent, IHandsComponent, IBodyPartAdded, IBodyPartRemoved, IDisarmedAct
    {
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);

            switch (message)
            {
                case PullAttemptMessage msg:
                    AttemptPull(msg);
                    break;
                case PullStartedMessage:
                    StartPulling();
                    break;
                case PullStoppedMessage:
                    StopPulling();
                    break;
            }
        }

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel channel, ICommonSession? session = null)
        {
            base.HandleNetworkMessage(message, channel, session);

            switch (message)
            {
                case ClientChangedHandMsg msg:
                    ActiveHand = msg.HandName;
                    break;
                case ClientAttackByInHandMsg msg:
                    InteractHandWithActiveHand(msg.HandName);
                    break;
                case UseInHandMsg:
                    UseActiveHeldEntity();
                    break;
                case ActivateInHandMsg msg:
                    ActivateHeldEntity(msg.HandName);
                    break;
                case MoveItemFromHandMsg msg:
                    TryMoveHeldEntityToActiveHand(msg.HandName);
                    break;
            }
        }

        protected override void OnHeldEntityRemovedFromHand(IEntity heldEntity, HandState handState)
        {
            if (heldEntity.TryGetComponent(out ItemComponent? item))
            {
                item.RemovedFromSlot();
                _entitySystemManager.GetEntitySystem<InteractionSystem>().UnequippedHandInteraction(Owner, heldEntity, handState);
            }
            if (heldEntity.TryGetComponent(out SpriteComponent? sprite))
            {
                sprite.RenderOrder = heldEntity.EntityManager.CurrentTick.Value;
            }
        }

        protected override void DoEquippedHandInteraction(IEntity entity, HandState handState)
        {
            _entitySystemManager.GetEntitySystem<InteractionSystem>().EquippedHandInteraction(Owner, entity, handState);
        }

        protected override void DoDroppedInteraction(IEntity heldEntity, bool intentionalDrop)
        {
            _entitySystemManager.GetEntitySystem<InteractionSystem>().DroppedInteraction(Owner, heldEntity, intentionalDrop);
        }

        protected override void DoHandSelectedInteraction(IEntity entity)
        {
            _entitySystemManager.GetEntitySystem<InteractionSystem>().HandSelectedInteraction(Owner, entity);
        }

        protected override void DoHandDeselectedInteraction(IEntity entity)
        {
            _entitySystemManager.GetEntitySystem<InteractionSystem>().HandDeselectedInteraction(Owner, entity);
        }

        protected override async void DoInteraction(IEntity activeHeldEntity, IEntity heldEntity)
        {
            await _entitySystemManager.GetEntitySystem<InteractionSystem>()
                .Interaction(Owner, activeHeldEntity, heldEntity, EntityCoordinates.Invalid);
        }

        protected override void DoActivate(IEntity heldEntity)
        {
            _entitySystemManager.GetEntitySystem<InteractionSystem>()
                .TryInteractionActivate(Owner, heldEntity);
        }

        protected override void DoUse(IEntity heldEntity)
        {
            _entitySystemManager.GetEntitySystem<InteractionSystem>()
                .TryUseInteraction(Owner, heldEntity);
        }

        protected override void HandlePickupAnimation(IEntity entity)
        {
            var pickupDirection = Owner.Transform.WorldPosition;

            var outermostEntity = entity;
            while (outermostEntity.TryGetContainer(out var container))
                outermostEntity = container.Owner;

            var initialPosition = outermostEntity.Transform.Coordinates;

            if (pickupDirection == initialPosition.ToMapPos(Owner.EntityManager))
                return;

            SendNetworkMessage(new PickupAnimationMessage(entity.Uid, pickupDirection, initialPosition));
        }

        #region Pull/Disarm

        void IBodyPartAdded.BodyPartAdded(BodyPartAddedEventArgs args)
        {
            if (args.Part.PartType != BodyPartType.Hand)
                return;

            var handLocation = ReadOnlyHands.Count == 0 ? HandLocation.Right : HandLocation.Left; //TODO: make hand body part have a handlocation?

            AddHand(args.Slot, handLocation);
        }

        void IBodyPartRemoved.BodyPartRemoved(BodyPartRemovedEventArgs args)
        {
            if (args.Part.PartType != BodyPartType.Hand)
                return;

            RemoveHand(args.Slot);
        }

        bool IDisarmedAct.Disarmed(DisarmedActEventArgs eventArgs)
        {
            if (BreakPulls())
                return false;

            var source = eventArgs.Source;
            var target = eventArgs.Target;

            if (source != null)
            {
                SoundSystem.Play(Filter.Pvs(source), "/Audio/Effects/thudswoosh.ogg", source,
                    AudioHelpers.WithVariation(0.025f));

                if (target != null)
                {
                    if (ActiveHand != null && Drop(ActiveHand, false))
                    {
                        source.PopupMessageOtherClients(Loc.GetString("{0} disarms {1}!", source.Name, target.Name));
                        source.PopupMessageCursor(Loc.GetString("You disarm {0}!", target.Name));
                    }
                    else
                    {
                        source.PopupMessageOtherClients(Loc.GetString("{0} shoves {1}!", source.Name, target.Name));
                        source.PopupMessageCursor(Loc.GetString("You shove {0}!", target.Name));
                    }
                }
            }

            return true;
        }

        // We want this to be the last disarm act to run.
        int IDisarmedAct.Priority => int.MaxValue;

        private bool BreakPulls()
        {
            // What is this API??
            if (!Owner.TryGetComponent(out SharedPullerComponent? puller)
                || puller.Pulling == null || !puller.Pulling.TryGetComponent(out PullableComponent? pullable))
                return false;

            return pullable.TryStopPull();
        }

        private void AttemptPull(PullAttemptMessage msg)
        {
            if (!ReadOnlyHands.Any(hand => hand.Enabled))
            {
                msg.Cancelled = true;
            }
        }

        private void StartPulling()
        {
            var firstFreeHand = Hands.FirstOrDefault(hand => hand.Enabled);

            if (firstFreeHand == null)
                return;

            DisableHand(firstFreeHand);
        }

        private void StopPulling()
        {
            var firstOccupiedHand = Hands.FirstOrDefault(hand => !hand.Enabled);

            if (firstOccupiedHand == null)
                return;

            EnableHand(firstOccupiedHand);
        }

        #endregion

        #region Old public methods

        public IEnumerable<string> HandNames => ReadOnlyHands.Select(h => h.Name);

        public int Count => ReadOnlyHands.Count;

        /// <summary>
        ///     Returns a list of all hand names, with the active hand being first.
        /// </summary>
        public IEnumerable<string> ActivePriorityEnumerable()
        {
            if (ActiveHand != null)
                yield return ActiveHand;

            foreach (var hand in ReadOnlyHands)
            {
                if (hand.Name == ActiveHand || !hand.Enabled)
                    continue;

                yield return hand.Name;
            }
        }

        /// <summary>
        ///     Attempts to use the active held item.
        /// </summary>
        public void ActivateItem()
        {
            UseActiveHeldEntity();
        }

        /// <summary>
        ///     Tries to drop the contents of a hand directly under the player.
        /// </summary>
        public bool Drop(string handName, bool checkActionBlocker = true, bool intentionalDrop = true)
        {
            return TryDropHandToFloor(handName, checkActionBlocker, intentionalDrop);
        }

        /// <summary>
        ///     Tries to drop an entity in a hand directly under the player.
        /// </summary>
        public bool Drop(IEntity entity, bool checkActionBlocker = true, bool intentionalDrop = true)
        {
            return TryDropEntityToFloor(entity, checkActionBlocker, intentionalDrop);
        }

        /// <summary>
        ///     Tries to unequip contents of a hand directly into a container.
        /// </summary>
        public bool Drop(IEntity entity, BaseContainer targetContainer, bool checkActionBlocker = true)
        {
            return TryPutEntityIntoContainer(entity, targetContainer, checkActionBlocker);
        }

        /// <summary>
        ///     Tries to get the ItemComponent on the entity held by a hand.
        /// </summary>
        public ItemComponent? GetItem(string handName)
        {
            if (!TryGetHeldEntity(handName, out var heldEntity))
                return null;

            heldEntity.TryGetComponent(out ItemComponent? item);
            return item;
        }

        /// <summary>
        ///     Tries to get the ItemComponent on the entity held by a hand.
        /// </summary>
        public bool TryGetItem(string handName, [NotNullWhen(true)] out ItemComponent? item)
        {
            item = null;

            if (!TryGetHeldEntity(handName, out var heldEntity))
                return false;

            return heldEntity.TryGetComponent(out item);
        }

        /// <summary>
        ///     Tries to get the ItemComponent off the entity in the active hand.
        /// </summary>
        public ItemComponent? GetActiveHand
        {
            get
            {
                if (!TryGetActiveHeldEntity(out var heldEntity))
                    return null;

                heldEntity.TryGetComponent(out ItemComponent? item);
                return item;
            }
        }

        public IEnumerable<ItemComponent> GetAllHeldItems()
        {
            foreach (var entity in GetAllHeldEntities())
            {
                if (entity.TryGetComponent(out ItemComponent? item))
                    yield return item;
            }
        }

        /// <summary>
        ///     Checks if any hand can pick up an item.
        /// </summary>
        public bool CanPutInHand(ItemComponent item, bool mobCheck = true)
        {
            var entity = item.Owner;

            if (mobCheck && !PlayerCanPickup())
                return false;

            foreach (var hand in Hands)
            {
                if (CanInsertEntityIntoHand(hand, entity))
                    return true;
            }
            return false;
        }

        /// <summary>
        ///     Attempts to put an item into the active hand, or any other hand if it cannot.
        /// </summary>
        public bool PutInHand(ItemComponent item, bool checkActionBlocker = true)
        {
            return TryPutInActiveHandOrAny(item.Owner, checkActionBlocker);
        }

        /// <summary>
        ///     Puts an item any hand, prefering the active hand, or puts it on the floor under the player.
        /// </summary>
        public void PutInHandOrDrop(ItemComponent item, bool checkActionBlocker = true)
        {
            var entity = item.Owner;

            if (!TryPutInActiveHandOrAny(entity, checkActionBlocker))
                entity.Transform.Coordinates = Owner.Transform.Coordinates;
        }

        #endregion
    }
}
