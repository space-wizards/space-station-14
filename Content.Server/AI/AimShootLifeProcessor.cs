using System;
using System.Collections.Generic;
using Content.Server.Interfaces.GameObjects.Components.Movement;
using Content.Shared.Physics;
using Robust.Server.AI;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Server.AI
{
    /// <summary>
    ///     The object stays stationary. The object will periodically scan for *any* life forms in its radius, and engage them.
    ///     The object will rotate itself to point at the locked entity, and if it has a weapon will shoot at the entity.
    /// </summary>
    [AiLogicProcessor("AimShootLife")]
    class AimShootLifeProcessor : AiLogicProcessor
    {
#pragma warning disable 649
        [Dependency] private readonly IPhysicsManager _physMan;
        [Dependency] private readonly IServerEntityManager _entMan;
        [Dependency] private readonly IGameTiming _timeMan;
#pragma warning restore 649

        private readonly List<IEntity> _workList = new List<IEntity>();

        private const float MaxAngSpeed = (float)(Math.PI / 2); // how fast our turret can rotate
        private const float ScanPeriod = 1.0f; // tweak this for performance and gameplay experience
        private float _lastScan;

        private IEntity _curTarget;

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            if (SelfEntity == null)
                return;

            DoScanning();
            DoTracking(frameTime);
        }

        private void DoScanning()
        {
            var curTime = _timeMan.CurTime.TotalSeconds;
            if (curTime - _lastScan > ScanPeriod)
            {
                _lastScan = (float)curTime;
                _curTarget = FindBestTarget();
            }
        }

        private void DoTracking(float frameTime)
        {
            // not valid entity to target.
            if (_curTarget == null || !_curTarget.IsValid())
            {
                _curTarget = null;
                return;
            }

            // point me at the target
            var tarPos = _curTarget.GetComponent<ITransformComponent>().WorldPosition;
            var selfTransform = SelfEntity.GetComponent<ITransformComponent>();
            var myPos = selfTransform.WorldPosition;

            var curDir = selfTransform.LocalRotation.ToVec();
            var tarDir = (tarPos - myPos).Normalized;

            var fwdAng = Vector2.Dot(curDir, tarDir);

            Vector2 newDir;
            if (fwdAng < 0) // target behind turret, just rotate in a direction to get target in front
            {
                var curRight = new Vector2(-curDir.Y, curDir.X); // right handed coord system
                var rightAngle = Vector2.Dot(curDir, new Vector2(-tarDir.Y, tarDir.X)); // right handed coord system
                var rotateSign = -Math.Sign(rightAngle);
                newDir = curDir + curRight * rotateSign * MaxAngSpeed * frameTime;
            }
            else // target in front, adjust to aim at him
            {
                newDir = MoveTowards(curDir, tarDir, MaxAngSpeed, frameTime);
            }

            selfTransform.LocalRotation = new Angle(newDir);

            if (fwdAng > -0.9999)
            {
                // TODO: shoot gun, prob need aimbot because entity rotation lags behind moving target
            }
        }

        private IEntity FindBestTarget()
        {
            // "best" target is the closest one with LOS

            var ents = _entMan.GetEntitiesInRange(SelfEntity, VisionRadius);
            var myTransform = SelfEntity.GetComponent<ITransformComponent>();
            var maxRayLen = VisionRadius * 2.5f; // circle inscribed in square, square diagonal = 2*r*sqrt(2)

            _workList.Clear();
            foreach (var entity in ents)
            {
                // filter to "people" entities (entities with controllers)
                if (!entity.HasComponent<IMoverComponent>())
                    continue;

                // build the ray
                var dir = entity.GetComponent<ITransformComponent>().WorldPosition - myTransform.WorldPosition;
                var ray = new Ray(myTransform.WorldPosition, dir.Normalized, (int)(CollisionGroup.MobImpassable | CollisionGroup.Impassable));

                // cast the ray
                var result = _physMan.IntersectRay(ray, maxRayLen);

                // add to visible list
                if (result.HitEntity == entity)
                    _workList.Add(entity);
            }

            // get closest entity in list
            var closestEnt = GetClosest(myTransform.WorldPosition, _workList);

            // return closest
            return closestEnt;
        }

        private static IEntity GetClosest(Vector2 origin, IEnumerable<IEntity> list)
        {
            IEntity closest = null;
            var minDistSqrd = float.PositiveInfinity;

            foreach (var ent in list)
            {
                var pos = ent.GetComponent<ITransformComponent>().WorldPosition;
                var distSqrd = (pos - origin).LengthSquared;

                if (distSqrd > minDistSqrd)
                    continue;

                closest = ent;
                minDistSqrd = distSqrd;
            }

            return closest;
        }

        private static Vector2 MoveTowards(Vector2 current, Vector2 target, float speed, float delta)
        {
            var maxDeltaDist = speed * delta;
            var a = target - current;
            var magnitude = a.Length;
            if (magnitude <= maxDeltaDist)
            {
                return target;
            }

            return current + a / magnitude * maxDeltaDist;
        }
    }
}
