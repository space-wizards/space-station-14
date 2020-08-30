using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces.GameObjects.Components.Items;
using Content.Server.Throw;
using Content.Server.Utility;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Items;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Items.Storage
{
    [RegisterComponent]
    [ComponentReference(typeof(StorableComponent))]
    [ComponentReference(typeof(IItemComponent))]
    public class ItemComponent : StorableComponent, IInteractHand, IExAct, IEquipped, IUnequipped, IItemComponent
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

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

        public void Equipped(EquippedEventArgs eventArgs)
        {
            EquippedToSlot();
        }

        public void Unequipped(UnequippedEventArgs eventArgs)
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

            if (Owner.TryGetComponent(out ICollidableComponent physics) &&
                physics.Anchored)
            {
                return false;
            }

            var itemPos = Owner.Transform.MapPosition;

            return InteractionChecks.InRangeUnobstructed(user, itemPos, ignoredEnt: Owner, ignoreInsideBlocker:true);
        }

        public bool InteractHand(InteractHandEventArgs eventArgs)
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
                    ContainerHelpers.IsInContainer(component.Owner) ||
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

        public override ComponentState GetComponentState()
        {
            return new ItemComponentState(EquippedPrefix);
        }

        public void OnExplosion(ExplosionEventArgs eventArgs)
        {
            var sourceLocation = eventArgs.Source;
            var targetLocation = eventArgs.Target.Transform.GridPosition;
            var dirVec = (targetLocation.ToMapPos(_mapManager) - sourceLocation.ToMapPos(_mapManager)).Normalized;

            var throwForce = 1.0f;

            switch (eventArgs.Severity)
            {
                case ExplosionSeverity.Destruction:
                    throwForce = 3.0f;
                    break;
                case ExplosionSeverity.Heavy:
                    throwForce = 2.0f;
                    break;
                case ExplosionSeverity.Light:
                    throwForce = 1.0f;
                    break;
            }

            ThrowHelper.Throw(Owner, throwForce, targetLocation, sourceLocation, true);
        }
    }
}
