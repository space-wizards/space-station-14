using Content.Server.Interfaces.GameObjects;
using SS14.Server.Interfaces.GameObjects;
using Content.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using Content.Server.GameObjects.EntitySystems;
using SS14.Shared.GameObjects;
using System;
using Content.Shared.GameObjects.Components.Items;

namespace Content.Server.GameObjects
{
    public class ItemComponent : StoreableComponent, IAttackHand
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
    }
}
