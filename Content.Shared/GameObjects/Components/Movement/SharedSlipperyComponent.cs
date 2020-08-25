using System.Collections.Generic;
using System.Linq;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Physics;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Movement
{
    public abstract class SharedSlipperyComponent : Component, ICollideBehavior
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public sealed override string Name => "Slippery";

        /// <summary>
        ///     The list of entities that have been slipped by this component,
        ///     and which have not stopped colliding with its owner yet.
        /// </summary>
        protected readonly List<EntityUid> _slipped = new List<EntityUid>();

        /// <summary>
        ///     How many seconds the mob will be paralyzed for.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        private float ParalyzeTime { get; set; } = 3f;

        /// <summary>
        ///     Percentage of shape intersection for a slip to occur.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        private float IntersectPercentage { get; set; } = 0.3f;

        /// <summary>
        ///     Entities will only be slipped if their speed exceeds this limit.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        private float RequiredSlipSpeed { get; set; } = 0f;

        private bool TrySlip(IEntity entity)
        {
            if (ContainerHelpers.IsInContainer(Owner)
                ||  _slipped.Contains(entity.Uid)
                ||  !entity.TryGetComponent(out SharedStunnableComponent stun)
                ||  !entity.TryGetComponent(out ICollidableComponent otherBody)
                ||  !Owner.TryGetComponent(out ICollidableComponent body))
            {
                return false;
            }

            if (otherBody.LinearVelocity.Length < RequiredSlipSpeed || stun.KnockedDown)
            {
                return false;
            }

            var percentage = otherBody.WorldAABB.IntersectPercentage(body.WorldAABB);

            if (percentage < IntersectPercentage)
            {
                return false;
            }

            if (!EffectBlockerSystem.CanSlip(entity))
            {
                return false;
            }

            if (entity.TryGetComponent(out ICollidableComponent collidable))
            {
                var controller = collidable.EnsureController<SlipController>();
                controller.LinearVelocity = collidable.LinearVelocity;
            }

            stun.Paralyze(5);
            _slipped.Add(entity.Uid);

            OnSlip();

            return true;
        }

        protected virtual void OnSlip() { }

        public void CollideWith(IEntity collidedWith)
        {
            TrySlip(collidedWith);
        }

        public void Update()
        {
            foreach (var uid in _slipped.ToArray())
            {
                if (!uid.IsValid() || !_entityManager.EntityExists(uid))
                {
                    continue;
                }

                var entity = _entityManager.GetEntity(uid);
                var collidable = Owner.GetComponent<ICollidableComponent>();
                var otherCollidable = entity.GetComponent<ICollidableComponent>();

                if (!collidable.WorldAABB.Intersects(otherCollidable.WorldAABB))
                {
                    _slipped.Remove(uid);
                }
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            var collidable = Owner.EnsureComponent<CollidableComponent>();

            collidable.Hard = false;

            var shape = collidable.PhysicsShapes.FirstOrDefault();

            if (shape != null)
            {
                shape.CollisionLayer |= (int) CollisionGroup.SmallImpassable;
                shape.CollisionMask = (int) CollisionGroup.None;
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => ParalyzeTime, "paralyzeTime", 3f);
            serializer.DataField(this, x  => IntersectPercentage, "intersectPercentage", 0.3f);
            serializer.DataField(this, x => RequiredSlipSpeed, "requiredSlipSpeed", 0f);
        }
    }
}
