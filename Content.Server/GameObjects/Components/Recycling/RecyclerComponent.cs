#nullable enable
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Conveyor;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Recycling;
using Content.Shared.Physics;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Components.Map;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Recycling
{
    // TODO: Add sound and safe beep
    [RegisterComponent]
    public class RecyclerComponent : Component, ICollideBehavior
    {
        public override string Name => "Recycler";

        private readonly List<IEntity> _intersecting = new();

        /// <summary>
        ///     Whether or not sentient beings will be recycled
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        private bool _safe;

        /// <summary>
        ///     The percentage of material that will be recovered
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        private float _efficiency;

        private bool Powered =>
            !Owner.TryGetComponent(out PowerReceiverComponent? receiver) ||
            receiver.Powered;

        private void Bloodstain()
        {
            if (Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(RecyclerVisuals.Bloody, true);
            }
        }

        private void Clean()
        {
            if (Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(RecyclerVisuals.Bloody, false);
            }
        }

        private bool CanGib(IEntity entity)
        {
            // We suppose this entity has a Recyclable component.
            return entity.HasComponent<IBody>() && !_safe && Powered;
        }

        private void Recycle(IEntity entity)
        {
            if (!_intersecting.Contains(entity))
            {
                _intersecting.Add(entity);
            }

            // TODO: Prevent collision with recycled items

            // Can only recycle things that are recyclable... And also check the safety of the thing to recycle.
            if (!entity.TryGetComponent(out RecyclableComponent? recyclable) || !recyclable.Safe && _safe) return;

            // Mobs are a special case!
            if (CanGib(entity))
            {
                entity.GetComponent<IBody>().Gib(true);
                Bloodstain();
                return;
            }

            recyclable.Recycle(_efficiency);
        }

        private bool CanRun()
        {
            if (Owner.TryGetComponent(out PowerReceiverComponent? receiver) &&
                !receiver.Powered)
            {
                return false;
            }

            if (Owner.HasComponent<ItemComponent>())
            {
                return false;
            }

            return true;
        }

        private bool CanMove(IEntity entity)
        {
            if (entity == Owner)
            {
                return false;
            }

            if (!entity.TryGetComponent(out IPhysicsComponent? physics) ||
                physics.Anchored)
            {
                return false;
            }

            if (entity.HasComponent<ConveyorComponent>())
            {
                return false;
            }

            if (entity.HasComponent<IMapGridComponent>())
            {
                return false;
            }

            if (entity.IsInContainer())
            {
                return false;
            }

            return true;
        }

        public void Update(float frameTime)
        {
            if (!CanRun())
            {
                _intersecting.Clear();
                return;
            }

            var direction = Vector2.UnitX;

            for (var i = _intersecting.Count - 1; i >= 0; i--)
            {
                var entity = _intersecting[i];

                if (entity.Deleted || !CanMove(entity) || !Owner.EntityManager.IsIntersecting(Owner, entity))
                {
                    _intersecting.RemoveAt(i);
                    continue;
                }

                if (entity.TryGetComponent(out IPhysicsComponent? physics))
                {
                    var controller = physics.EnsureController<ConveyedController>();
                    controller.Move(direction, frameTime, entity.Transform.WorldPosition - Owner.Transform.WorldPosition);
                }
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _safe, "safe", true);
            serializer.DataField(ref _efficiency, "efficiency", 0.25f);
        }

        void ICollideBehavior.CollideWith(IEntity collidedWith)
        {
            Recycle(collidedWith);
        }
    }
}
