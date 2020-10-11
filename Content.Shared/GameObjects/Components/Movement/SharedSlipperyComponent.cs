using System;
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
        public virtual float ParalyzeTime { get; set; } = 2f;

        /// <summary>
        ///     Percentage of shape intersection for a slip to occur.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public virtual float IntersectPercentage { get; set; } = 0.3f;

        /// <summary>
        ///     Entities will only be slipped if their speed exceeds this limit.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public virtual float RequiredSlipSpeed { get; set; } = 0f;

        /// <summary>
        ///     The entity's speed will be multiplied by this to slip it forwards.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public virtual float LaunchForwardsMultiplier { get; set; } = 1f;

        /// <summary>
        ///     Whether or not this component will try to slip entities.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public virtual bool Slippery { get; set; }

        private bool TrySlip(IEntity entity)
        {
            if (!Slippery
                || ContainerHelpers.IsInContainer(Owner)
                ||  _slipped.Contains(entity.Uid)
                ||  !entity.TryGetComponent(out SharedStunnableComponent stun)
                ||  !entity.TryGetComponent(out IPhysicsComponent otherBody)
                ||  !Owner.TryGetComponent(out IPhysicsComponent body))
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

            if (entity.TryGetComponent(out IPhysicsComponent physics))
            {
                var controller = physics.EnsureController<SlipController>();
                controller.LinearVelocity = physics.LinearVelocity * LaunchForwardsMultiplier;
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
                    _slipped.Remove(uid);
                    continue;
                }

                var entity = _entityManager.GetEntity(uid);
                var physics = Owner.GetComponent<IPhysicsComponent>();
                var otherPhysics = entity.GetComponent<IPhysicsComponent>();

                if (!physics.WorldAABB.Intersects(otherPhysics.WorldAABB))
                {
                    _slipped.Remove(uid);
                }
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            var physics = Owner.EnsureComponent<PhysicsComponent>();

            physics.Hard = false;

            var shape = physics.PhysicsShapes.FirstOrDefault();

            if (shape != null)
            {
                shape.CollisionLayer |= (int) CollisionGroup.SmallImpassable;
                shape.CollisionMask = (int) CollisionGroup.None;
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => x.ParalyzeTime, "paralyzeTime", 3f);
            serializer.DataField(this, x  => x.IntersectPercentage, "intersectPercentage", 0.3f);
            serializer.DataField(this, x => x.RequiredSlipSpeed, "requiredSlipSpeed", 0f);
            serializer.DataField(this, x => x.LaunchForwardsMultiplier, "launchForwardsMultiplier", 1f);
            serializer.DataField(this, x => x.Slippery, "slippery", true);
        }
    }

    [Serializable, NetSerializable]
    public class SlipperyComponentState : ComponentState
    {
        public float ParalyzeTime { get; }
        public float IntersectPercentage { get; }
        public float RequiredSlipSpeed { get; }
        public float LaunchForwardsMultiplier { get; }
        public bool Slippery { get; }

        public SlipperyComponentState(float paralyzeTime, float intersectPercentage, float requiredSlipSpeed, float launchForwardsMultiplier, bool slippery) : base(ContentNetIDs.SLIP)
        {
            ParalyzeTime = paralyzeTime;
            IntersectPercentage = intersectPercentage;
            RequiredSlipSpeed = requiredSlipSpeed;
            LaunchForwardsMultiplier = launchForwardsMultiplier;
            Slippery = slippery;
        }
    }
}
