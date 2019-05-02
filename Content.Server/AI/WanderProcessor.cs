using System;
using Content.Server.GameObjects.Components.Movement;
using Content.Shared.Physics;
using JetBrains.Annotations;
using Robust.Server.AI;
using Robust.Server.GameObjects;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Server.AI
{
    /// <summary>
    ///     Designed to control a mob. The mob will wander around, then idke at a the destination for awhile.
    /// </summary>
    [AiLogicProcessor("Wander")]
    class WanderProcessor : AiLogicProcessor
    {
        [Dependency, UsedImplicitly] private readonly IPhysicsManager _physMan;
        [Dependency, UsedImplicitly] private readonly IServerEntityManager _entMan;
        [Dependency, UsedImplicitly] private readonly IGameTiming _timeMan;

        private static readonly TimeSpan IdleTimeSpan = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan WalkingTimeout = TimeSpan.FromSeconds(3);
        private const float MaxWalkDistance = 3; // meters
        private const float AdditionalIdleTime = 2; // 0 to this many more seconds

        private FsmState _CurrentState;
        private TimeSpan _startStateTime;
        private Vector2 _walkTargetPos;

        public override void Update(float frameTime)
        {
            if (SelfEntity == null)
                return;

            ProcessState();
        }

        private void ProcessState()
        {
            switch (_CurrentState)
            {
                case FsmState.None:
                    _CurrentState = FsmState.Idle;
                    break;
                case FsmState.Idle:
                    IdleState();
                    break;
                case FsmState.Walking:
                    WalkingState();
                    break;
            }
        }

        private void IdlePositiveEdge(uint rngState)
        {
            _startStateTime = _timeMan.CurTime + IdleTimeSpan + TimeSpan.FromSeconds(Random01(ref rngState) * AdditionalIdleTime);
            _CurrentState = FsmState.Idle;
        }

        private void IdleState()
        {
            if (_timeMan.CurTime < _startStateTime + IdleTimeSpan)
                return;

            var entWorldPos = SelfEntity.Transform.WorldPosition;

            if (SelfEntity.TryGetComponent<BoundingBoxComponent>(out var bounds))
                entWorldPos = bounds.WorldAABB.Center;


            var rngState = _timeMan.CurTick.Value + 1;
            for (var i = 0; i < 3; i++) // you get 3 chances to find a place to walk
            {
                var dir = new Vector2(Random01(ref rngState) * 2 - 1, Random01(ref rngState) *2 -1);
                var ray = new Ray(entWorldPos, dir, (int) CollisionGroup.Grid);
                var rayResult = _physMan.IntersectRay(ray, MaxWalkDistance, SelfEntity);
                
                if (rayResult.DidHitObject && rayResult.Distance > 1) // hit an impassable object
                {
                    // set the new position back from the wall a bit
                    _walkTargetPos = entWorldPos + dir * (rayResult.Distance - 0.5f);
                    WalkingPositiveEdge();
                    return;
                }

                if (!rayResult.DidHitObject) // hit nothing (path clear)
                {
                    _walkTargetPos = dir * MaxWalkDistance;
                    WalkingPositiveEdge();
                    return;
                }
            }

            // can't find clear spot, do nothing, sleep longer
            _startStateTime = _timeMan.CurTime;
        }

        private void WalkingPositiveEdge()
        {
            _startStateTime = _timeMan.CurTime;
            _CurrentState = FsmState.Walking;
        }

        private void WalkingState()
        {
            var rngState = _timeMan.CurTick.Value + 1;
            if (_timeMan.CurTime > _startStateTime + WalkingTimeout) // walked too long, go idle
            {
                IdlePositiveEdge(rngState);
                return;
            }

            var targetDiff = _walkTargetPos - SelfEntity.Transform.WorldPosition;

            if (targetDiff.LengthSquared < 0.1) // close enough
            {
                // stop walking
                if (SelfEntity.TryGetComponent<PhysicsComponent>(out var phys))
                {
                    phys.LinearVelocity = Vector2.Zero;
                }

                IdlePositiveEdge(rngState);
                return;
            }

            // continue walking
            if (SelfEntity.TryGetComponent<PhysicsComponent>(out var physics))
            {
                var moveSpeed = 2.0f;
                if (SelfEntity.TryGetComponent<PlayerInputMoverComponent>(out var mover))
                    moveSpeed = mover.WalkMoveSpeed;

                var velDiff = targetDiff.Normalized * moveSpeed - physics.LinearVelocity; // to - from
                var diffSpeed = velDiff.Length;

                var speedmod = moveSpeed / diffSpeed;
                speedmod = FloatMath.Min(speedmod, moveSpeed);

                physics.LinearVelocity += velDiff * speedmod;
            }
        }

        private float Random01(ref uint state)
        {
            DebugTools.Assert(state != 0);

            //xorshift32
            var x = state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            state = x;
            return x / (float)uint.MaxValue;
        }

        private enum FsmState
        {
            None,
            Idle,
            Walking,
        }
    }
}
