using System;
using Content.Server.GameObjects.Components;
using Content.Server.GameObjects.Components.Destructible;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces.GameObjects;
using Content.Server.Throw;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Items;
using Content.Shared.Physics;
using Robust.Server.GameObjects;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects
{
    [RegisterComponent]
    [ComponentReference(typeof(StoreableComponent))]
    public class ItemComponent : StoreableComponent, IAttackHand, IExAct
    {
        public override string Name => "Item";
        public override uint? NetID => ContentNetIDs.ITEM;

        #pragma warning disable 649
        [Dependency] private readonly IRobustRandom _robustRandom;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager;
        [Dependency] private readonly IMapManager _mapManager;
        #pragma warning restore 649

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

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _equippedPrefix, "HeldPrefix", null);
        }

        public bool CanPickup(IEntity user)
        {
            if (!ActionBlockerSystem.CanPickup(user)) return false;

            if (user.Transform.MapID != Owner.Transform.MapID)
                return false;

            var userPos = user.Transform.MapPosition;
            var itemPos = Owner.Transform.WorldPosition;

            return _entitySystemManager.GetEntitySystem<InteractionSystem>()
                .InRangeUnobstructed(userPos, itemPos, ignoredEnt: Owner);
        }

        public bool AttackHand(AttackHandEventArgs eventArgs)
        {
            if (!CanPickup(eventArgs.User)) return false;

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

            protected override VerbVisibility GetVisibility(IEntity user, ItemComponent component)
            {
                if (ContainerHelpers.IsInContainer(component.Owner) || !component.CanPickup(user))
                {
                    return VerbVisibility.Invisible;
                }

                return VerbVisibility.Visible;
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
                return (_robustRandom.NextFloat() * size) - size / 2;
            }
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
