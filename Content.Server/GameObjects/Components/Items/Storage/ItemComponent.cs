using Content.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.GameObjects;
using Content.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using System;
using Content.Shared.GameObjects.Components.Items;
using Content.Server.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Maths;

namespace Content.Server.GameObjects
{
    public class ItemComponent : StoreableComponent, IAttackHand, IAfterAttack
    {
        public override string Name => "Item";
        public override uint? NetID => ContentNetIDs.ITEM;
        public override Type StateType => typeof(ItemComponentState);

        private string _equippedPrefix;

        public string EquippedPrefix
        {
            get
            {
                return _equippedPrefix;
            }
            set
            {
                Dirty();
                _equippedPrefix = value;
            }
        }

        public void RemovedFromSlot()
        {
            foreach (var component in Owner.GetAllComponents<ISpriteRenderableComponent>())
            {
                component.Visible = true;
            }
        }

        public void EquippedToSlot()
        {
            foreach (var component in Owner.GetAllComponents<ISpriteRenderableComponent>())
            {
                component.Visible = false;
            }
        }

        public bool AttackHand(AttackHandEventArgs eventArgs)
        {
            var hands = eventArgs.User.GetComponent<IHandsComponent>();
            hands.PutInHand(this, hands.ActiveIndex, fallback: false);
            return true;
        }

        [Verb]
        public sealed class PickUpVerb : Verb<ItemComponent>
        {
            protected override string GetText(IEntity user, ItemComponent component)
            {
                if (user.TryGetComponent(out HandsComponent hands) && hands.IsHolding(component.Owner))
                {
                    return "Pick Up (Already Holding)";
                }
                return "Pick Up";
            }

            protected override bool IsDisabled(IEntity user, ItemComponent component)
            {
                if (user.TryGetComponent(out HandsComponent hands) && hands.IsHolding(component.Owner))
                {
                    return true;
                }
                return false;
            }

            protected override void Activate(IEntity user, ItemComponent component)
            {
                if (user.TryGetComponent(out HandsComponent hands) && !hands.IsHolding(component.Owner))
                {
                    hands.PutInHand(component);
                }
            }
        }

        public override ComponentState GetComponentState()
        {
            return new ItemComponentState(EquippedPrefix);
        }

        public void AfterAttack(AfterAttackEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent<HandsComponent>(out var handComponent))
            {
                return;
            }
            if (eventArgs.Attacked == null || !eventArgs.Attacked.TryGetComponent<PlaceableSurfaceComponent>(out var placeableSurfaceComponent))
            {
                return;
            }
            handComponent.Drop(handComponent.ActiveIndex);
            Owner.Transform.WorldPosition = eventArgs.ClickLocation.Position;
            return;
        }

        public void Fumble()
        {
            if (Owner.TryGetComponent<PhysicsComponent>(out var physicsComponent))
            {
                physicsComponent.LinearVelocity += RandomOffset();
            }
        }

        private Vector2 RandomOffset()
        {
            return new Vector2(RandomOffset(), RandomOffset());
            float RandomOffset()
            {
                var size = 15.0F;
                return (new Random().NextFloat() * size) - size / 2;
            }
        }
    }
}
