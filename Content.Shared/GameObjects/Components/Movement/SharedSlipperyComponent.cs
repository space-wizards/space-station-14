#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.EntitySystems.EffectBlocker;
using Content.Shared.Physics;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Movement
{
    public abstract class SharedSlipperyComponent : Component, ICollideBehavior
    {
        public sealed override string Name => "Slippery";

        protected float _paralyzeTime = 3f;
        protected float _intersectPercentage = 0.3f;
        protected float _requiredSlipSpeed = 0.1f;
        protected float _launchForwardsMultiplier = 1f;
        protected bool _slippery = true;

        /// <summary>
        ///     The list of entities that have been slipped by this component,
        ///     and which have not stopped colliding with its owner yet.
        /// </summary>
        protected readonly List<EntityUid> _slipped = new();

        /// <summary>
        ///     How many seconds the mob will be paralyzed for.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("paralyzeTime")]
        public float ParalyzeTime
        {
            get => _paralyzeTime;
            set
            {
                if (MathHelper.CloseTo(_paralyzeTime, value)) return;

                _paralyzeTime = value;
                Dirty();
            }
        }

        /// <summary>
        ///     Percentage of shape intersection for a slip to occur.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("intersectPercentage")]
        public float IntersectPercentage
        {
            get => _intersectPercentage;
            set
            {
                if (MathHelper.CloseTo(_intersectPercentage, value)) return;

                _intersectPercentage = value;
                Dirty();
            }
        }

        /// <summary>
        ///     Entities will only be slipped if their speed exceeds this limit.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("requiredSlipSpeed")]
        public float RequiredSlipSpeed
        {
            get => _requiredSlipSpeed;
            set
            {
                if (MathHelper.CloseTo(_requiredSlipSpeed, value)) return;

                _requiredSlipSpeed = value;
                Dirty();
            }
        }

        /// <summary>
        ///     The entity's speed will be multiplied by this to slip it forwards.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("launchForwardsMultiplier")]
        public float LaunchForwardsMultiplier
        {
            get => _launchForwardsMultiplier;
            set
            {
                if (MathHelper.CloseTo(_launchForwardsMultiplier, value)) return;

                _launchForwardsMultiplier = value;
                Dirty();
            }
        }

        /// <summary>
        ///     Whether or not this component will try to slip entities.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("slippery")]
        public bool Slippery
        {
            get => _slippery;
            set
            {
                if (_slippery == value) return;

                _slippery = value;
                Dirty();
            }
        }

        private bool TrySlip(IEntity entity)
        {
            if (!Slippery
                || Owner.IsInContainer()
                ||  _slipped.Contains(entity.Uid)
                ||  !entity.TryGetComponent(out SharedStunnableComponent? stun)
                ||  !entity.TryGetComponent(out IPhysicsComponent? otherBody)
                ||  !Owner.TryGetComponent(out IPhysicsComponent? body))
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

            if (entity.TryGetComponent(out IPhysicsComponent? physics))
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
                if (!uid.IsValid() || !Owner.EntityManager.EntityExists(uid))
                {
                    _slipped.Remove(uid);
                    continue;
                }

                var entity = Owner.EntityManager.GetEntity(uid);
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
