using System.Collections.Generic;
using System.Timers;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.Throw;
using Content.Shared.Audio;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Physics;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Server.GameObjects.Components.Movement
{
    [RegisterComponent]
    public class SlipperyComponent : Component, ICollideBehavior
    {
        [Dependency] private IEntityManager _entityManager = default!;

        public override string Name => "Slippery";

        private List<EntityUid> _slipped = new List<EntityUid>();

        /// <summary>
        ///     How many seconds the mob will be paralyzed for.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float ParalyzeTime { get; set; } = 3f;

        /// <summary>
        ///     Percentage of shape intersection for a slip to occur.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float IntersectPercentage { get; set; } = 0.3f;

        /// <summary>
        ///     Entities will only be slipped if their speed exceeds this limit.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float RequiredSlipSpeed { get; set; } = 0f;

        /// <summary>
        ///     Path to the sound to be played when a mob slips.
        /// </summary>
        [ViewVariables]
        public string SlipSound { get; set; } = "/Audio/Effects/slip.ogg";

        public override void Initialize()
        {
            base.Initialize();
            var collidable = Owner.GetComponent<ICollidableComponent>();

            collidable.Hard = false;
            var shape = collidable.PhysicsShapes[0];
            shape.CollisionLayer |= (int) CollisionGroup.SmallImpassable;
            shape.CollisionMask = (int)CollisionGroup.None;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(this, x => ParalyzeTime, "paralyzeTime", 3f);
            serializer.DataField(this, x  => IntersectPercentage, "intersectPercentage", 0.3f);
            serializer.DataField(this, x => RequiredSlipSpeed, "requiredSlipSpeed", 0f);
            serializer.DataField(this, x => SlipSound, "slipSound", "/Audio/Effects/slip.ogg");
        }

        public void CollideWith(IEntity collidedWith)
        {
            if (ContainerHelpers.IsInContainer(Owner)
                ||  _slipped.Contains(collidedWith.Uid)
                ||  !collidedWith.TryGetComponent(out StunnableComponent stun)
                ||  !collidedWith.TryGetComponent(out ICollidableComponent otherBody)
                ||  !collidedWith.TryGetComponent(out PhysicsComponent otherPhysics)
                ||  !Owner.TryGetComponent(out ICollidableComponent body))
                return;

            if (otherPhysics.LinearVelocity.Length < RequiredSlipSpeed || stun.KnockedDown)
                return;

            var percentage = otherBody.WorldAABB.IntersectPercentage(body.WorldAABB);

            if (percentage < IntersectPercentage)
                return;

            if(!EffectBlockerSystem.CanSlip(collidedWith))
                return;

            stun.Paralyze(5f);
            _slipped.Add(collidedWith.Uid);

            if(!string.IsNullOrEmpty(SlipSound))
                EntitySystem.Get<AudioSystem>().PlayFromEntity(SlipSound, Owner, AudioHelpers.WithVariation(0.2f));
        }

        public void Update(float frameTime)
        {
            foreach (var uid in _slipped.ToArray())
            {
                if(!uid.IsValid() || !_entityManager.EntityExists(uid)) continue;

                var entity = _entityManager.GetEntity(uid);
                var collidable = Owner.GetComponent<ICollidableComponent>();
                var otherCollidable = entity.GetComponent<ICollidableComponent>();

                if (!collidable.WorldAABB.Intersects(otherCollidable.WorldAABB))
                    _slipped.Remove(uid);
            }
        }
    }
}
