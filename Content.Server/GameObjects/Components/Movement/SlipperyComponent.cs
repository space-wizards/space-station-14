using System.Collections.Generic;
using Content.Server.GameObjects.Components.Mobs;
using Content.Shared.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Movement
{
    [RegisterComponent]
    public class SlipperyComponent : Component, ICollideBehavior
    {
        [Dependency] private IEntityManager _entityManager = default!;

        public override string Name => "Slippery";

        private List<EntityUid> _slipped = new List<EntityUid>();

        public override void Initialize()
        {
            base.Initialize();
            var collidable = Owner.GetComponent<ICollidableComponent>();

            collidable.Hard = false;
            var shape = collidable.PhysicsShapes[0];
            shape.CollisionMask |= (int) CollisionGroup.MobImpassable;
            shape.CollisionLayer = (int)CollisionGroup.None;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
        }

        public void CollideWith(IEntity collidedWith)
        {
            if (Owner.Transform.ParentUid == collidedWith.Uid
                ||  _slipped.Contains(collidedWith.Uid)
                ||  !collidedWith.TryGetComponent(out StunnableComponent stun)
                ||  !collidedWith.TryGetComponent(out ICollidableComponent otherBody)
                ||  !Owner.TryGetComponent(out ICollidableComponent body))
                return;

            var percentage = otherBody.WorldAABB.IntersectPercentage(body.WorldAABB);

            Logger.Info($"{percentage}");

            if (percentage < 0.2f)
                return;

            stun.Paralyze(5f);
            _slipped.Add(collidedWith.Uid);
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
