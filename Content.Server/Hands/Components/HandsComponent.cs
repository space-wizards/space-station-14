using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Act;
using Content.Server.Interaction;
using Content.Server.Items;
using Content.Server.Popups;
using Content.Server.Pulling;
using Content.Shared.Audio;
using Content.Shared.Body.Part;
using Content.Shared.Hands.Components;
using Content.Shared.Popups;
using Content.Shared.Pulling.Components;
using Content.Shared.Sound;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Hands.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedHandsComponent))]
#pragma warning disable 618
    public class HandsComponent : SharedHandsComponent, IBodyPartAdded, IBodyPartRemoved, IDisarmedAct
#pragma warning restore 618
    {
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

        [DataField("disarmedSound")] SoundSpecifier _disarmedSound = new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg");

        int IDisarmedAct.Priority => int.MaxValue; // We want this to be the last disarm act to run.

        protected override void OnHeldEntityRemovedFromHand(EntityUid heldEntity, HandState handState)
        {
            if (IoCManager.Resolve<IEntityManager>().TryGetComponent(heldEntity, out ItemComponent? item))
            {
                item.RemovedFromSlot();
                _entitySystemManager.GetEntitySystem<InteractionSystem>().UnequippedHandInteraction(Owner, heldEntity, handState);
            }
            if (IoCManager.Resolve<IEntityManager>().TryGetComponent(heldEntity, out SpriteComponent? sprite))
            {
                sprite.RenderOrder = IoCManager.Resolve<IEntityManager>().CurrentTick.Value;
            }
        }

        protected override void HandlePickupAnimation(EntityUid entity)
        {
            var initialPosition = EntityCoordinates.FromMap(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(Owner).Parent?.Owner ?? Owner, IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(entity).MapPosition);

            var finalPosition = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(Owner).LocalPosition;

            if (finalPosition.EqualsApprox(initialPosition.Position))
                return;

            IoCManager.Resolve<IEntityManager>().EntityNetManager!.SendSystemNetworkMessage(
                new PickupAnimationMessage(entity, finalPosition, initialPosition));
        }

        #region Pull/Disarm

        void IBodyPartAdded.BodyPartAdded(BodyPartAddedEventArgs args)
        {
            if (args.Part.PartType != BodyPartType.Hand)
                return;

            // If this annoys you, which it should.
            // Ping Smugleaf.
            var location = args.Part.Symmetry switch
            {
                BodyPartSymmetry.None => HandLocation.Middle,
                BodyPartSymmetry.Left => HandLocation.Left,
                BodyPartSymmetry.Right => HandLocation.Right,
                _ => throw new ArgumentOutOfRangeException()
            };

            AddHand(args.Slot, location);
        }

        void IBodyPartRemoved.BodyPartRemoved(BodyPartRemovedEventArgs args)
        {
            if (args.Part.PartType != BodyPartType.Hand)
                return;

            RemoveHand(args.Slot);
        }

        bool IDisarmedAct.Disarmed(DisarmedActEvent @event)
        {
            if (BreakPulls())
                return false;

            var source = @event.Source;
            var target = @event.Target;

            if (source != null)
            {
                SoundSystem.Play(Filter.Pvs(source), _disarmedSound.GetSound(), source, AudioHelpers.WithVariation(0.025f));

                if (target != null)
                {
                    if (ActiveHand != null && Drop(ActiveHand, false))
                    {
                        source.PopupMessageOtherClients(Loc.GetString("hands-component-disarm-success-others-message", ("disarmer", Name: IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(source).EntityName), ("disarmed", Name: IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(target).EntityName)));
                        source.PopupMessageCursor(Loc.GetString("hands-component-disarm-success-message", ("disarmed", Name: IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(target).EntityName)));
                    }
                    else
                    {
                        source.PopupMessageOtherClients(Loc.GetString("hands-component-shove-success-others-message", ("shover", Name: IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(source).EntityName), ("shoved", Name: IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(target).EntityName)));
                        source.PopupMessageCursor(Loc.GetString("hands-component-shove-success-message", ("shoved", Name: IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(target).EntityName)));
                    }
                }
            }

            return true;
        }

        private bool BreakPulls()
        {
            // What is this API??
            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(Owner, out SharedPullerComponent? puller)
                || puller.Pulling == null || !IoCManager.Resolve<IEntityManager>().TryGetComponent(puller.Pulling, out SharedPullableComponent? pullable))
                return false;

            return _entitySystemManager.GetEntitySystem<PullingSystem>().TryStopPull(pullable);
        }

        #endregion

        #region Old public methods

        public IEnumerable<string> HandNames => Hands.Select(h => h.Name);

        public int Count => Hands.Count;

        /// <summary>
        ///     Returns a list of all hand names, with the active hand being first.
        /// </summary>
        public IEnumerable<string> ActivePriorityEnumerable()
        {
            if (ActiveHand != null)
                yield return ActiveHand;

            foreach (var hand in Hands)
            {
                if (hand.Name == ActiveHand)
                    continue;

                yield return hand.Name;
            }
        }

        /// <summary>
        ///     Tries to get the ItemComponent on the entity held by a hand.
        /// </summary>
        public ItemComponent? GetItem(string handName)
        {
            if (!TryGetHeldEntity(handName, out var heldEntity))
                return null;

            IoCManager.Resolve<IEntityManager>().TryGetComponent(heldEntity, out ItemComponent? item);
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

            return IoCManager.Resolve<IEntityManager>().TryGetComponent(heldEntity, out item);
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

                IoCManager.Resolve<IEntityManager>().TryGetComponent(heldEntity, out ItemComponent? item);
                return item;
            }
        }

        public IEnumerable<ItemComponent> GetAllHeldItems()
        {
            foreach (var entity in GetAllHeldEntities())
            {
                if (IoCManager.Resolve<IEntityManager>().TryGetComponent(entity, out ItemComponent? item))
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
        #endregion
    }
}

