using Content.Server.GameObjects.Components.GUI;
using Content.Server.Interfaces.GameObjects.Components.Items;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Items;
using Content.Shared.GameObjects.Components.Storage;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Utility;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Players;
using Robust.Shared.Physics;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Items.Storage
{
    [RegisterComponent]
    [ComponentReference(typeof(StorableComponent))]
    [ComponentReference(typeof(SharedStorableComponent))]
    [ComponentReference(typeof(IItemComponent))]
    public class ItemComponent : StorableComponent, IInteractHand, IExAct, IEquipped, IUnequipped, IItemComponent
    {
        public override string Name => "Item";
        public override uint? NetID => ContentNetIDs.ITEM;

        private string _equippedPrefix;

        public string EquippedPrefix
        {
            get
            {
                return _equippedPrefix;
            }
            set
            {
                _equippedPrefix = value;
                Dirty();
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

        public virtual void Equipped(EquippedEventArgs eventArgs)
        {
            EquippedToSlot();
        }

        public virtual void Unequipped(UnequippedEventArgs eventArgs)
        {
            RemovedFromSlot();
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _equippedPrefix, "HeldPrefix", null);
        }

        public bool CanPickup(IEntity user)
        {
            if (!ActionBlockerSystem.CanPickup(user))
            {
                return false;
            }

            if (user.Transform.MapID != Owner.Transform.MapID)
            {
                return false;
            }

            if (Owner.TryGetComponent(out IPhysBody physics) &&
                physics.BodyType == BodyType.Static)
            {
                return false;
            }

            return user.InRangeUnobstructed(Owner, ignoreInsideBlocker: true, popup: true);
        }

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            if (!CanPickup(eventArgs.User)) return false;

            var hands = eventArgs.User.GetComponent<IHandsComponent>();
            hands.PutInHand(this, hands.ActiveHand, false);
            return true;
        }

        [Verb]
        public sealed class PickUpVerb : Verb<ItemComponent>
        {
            protected override void GetData(IEntity user, ItemComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user) ||
                    component.Owner.IsInContainer() ||
                    !component.CanPickup(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                data.Text = Loc.GetString("Pick Up");
            }

            protected override void Activate(IEntity user, ItemComponent component)
            {
                if (user.TryGetComponent(out HandsComponent hands) && !hands.IsHolding(component.Owner))
                {
                    hands.PutInHand(component);
                }
            }
        }

        public override ComponentState GetComponentState(ICommonSession session)
        {
            return new ItemComponentState(EquippedPrefix);
        }

        public void OnExplosion(ExplosionEventArgs eventArgs)
        {
            var sourceLocation = eventArgs.Source;
            var targetLocation = eventArgs.Target.Transform.Coordinates;
            var dirVec = (targetLocation.ToMapPos(Owner.EntityManager) - sourceLocation.ToMapPos(Owner.EntityManager)).Normalized;

            float throwForce;

            switch (eventArgs.Severity)
            {
                case ExplosionSeverity.Destruction:
                    throwForce = 30.0f;
                    break;
                case ExplosionSeverity.Heavy:
                    throwForce = 20.0f;
                    break;
                default:
                    throwForce = 10.0f;
                    break;
            }

            Owner.TryThrow(dirVec * throwForce);
        }
    }
}
