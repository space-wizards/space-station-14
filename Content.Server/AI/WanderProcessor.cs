using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Movement;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces.Chat;
using Content.Shared.Physics;
using Robust.Server.AI;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Utility;

namespace Content.Server.AI
{
    /// <summary>
    ///     Designed to control a mob. The mob will wander around, then idle at a the destination for awhile.
    /// </summary>
    [AiLogicProcessor("Wander")]
    class WanderProcessor : AiLogicProcessor
    {
#pragma warning disable 649
        [Dependency] private readonly IPhysicsManager _physMan;
        [Dependency] private readonly IGameTiming _timeMan;
        [Dependency] private readonly IChatManager _chatMan;
#pragma warning restore 649

        private static readonly TimeSpan IdleTimeSpan = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan WalkingTimeout = TimeSpan.FromSeconds(3);
        private static readonly TimeSpan DisabledTimeout = TimeSpan.FromSeconds(10);

        private static List<string> _normalAssistantConversation = new List<string>
        {
            "stat me",
            "roll it easy!",
            "waaaaaagh!!!",
            "red wonz go fasta",
            "FOR TEH EMPRAH",
            "lol2cat",
            "dem dwarfs man, dem dwarfs",
            "SPESS MAHREENS",
            "hwee did eet fhor khayosss",
            "lifelike texture ;_;",
            "luv can bloooom",
            "PACKETS!!!",
            "SARAH HALE DID IT!!!",
            "Don't tell Chase",
            "not so tough now huh",
            "WERE NOT BAY!!",
            "IF YOU DONT LIKE THE CYBORGS OR SLIMES WHY DONT YU O JUST MAKE YORE OWN!",
            "DONT TALK TO ME ABOUT BALANCE!!!!",
            "YOU AR JUS LAZY AND DUMB JAMITORS AND SERVICE ROLLS",
            "BLAME HOSHI!!!",
            "ARRPEE IZ DED!!!",
            "THERE ALL JUS MEATAFRIENDS!",
            "SOTP MESING WITH THE ROUNS SHITMAN!!!",
            "SKELINGTON IS 4 SHITERS!",
            "MOMMSI R THE WURST SCUM!!",
            "How do we engiener=",
            "try to live freely and automatically good bye",
            "why woud i take a pin pointner??",
            "How do I set up the. SHow do I set u p the Singu. how I the scrungularity????",
        };

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
                case FsmState.Disabled:
                    DisabledState();
                    break;
            }
        }

        private void IdlePositiveEdge(ref uint rngState)
        {
            _startStateTime = _timeMan.CurTime + IdleTimeSpan + TimeSpan.FromSeconds(Random01(ref rngState) * AdditionalIdleTime);
            _CurrentState = FsmState.Idle;

            EmitProfanity(ref rngState);
        }

        private void IdleState()
        {
            if (!ActionBlockerSystem.CanMove(SelfEntity))
            {
                DisabledPositiveEdge();
                return;
            }

            if (_timeMan.CurTime < _startStateTime + IdleTimeSpan)
                return;

            var entWorldPos = SelfEntity.Transform.WorldPosition;

            if (SelfEntity.TryGetComponent<CollidableComponent>(out var bounds))
                entWorldPos = ((IPhysBody) bounds).WorldAABB.Center;

            var rngState = GenSeed();
            for (var i = 0; i < 3; i++) // you get 3 chances to find a place to walk
            {
                var dir = new Vector2(Random01(ref rngState) * 2 - 1, Random01(ref rngState) *2 -1);
                var ray = new Ray(entWorldPos, dir, (int) CollisionGroup.Impassable);
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
            var rngState = GenSeed();
            if (_timeMan.CurTime > _startStateTime + WalkingTimeout) // walked too long, go idle
            {
                IdlePositiveEdge(ref rngState);
                return;
            }

            var targetDiff = _walkTargetPos - SelfEntity.Transform.WorldPosition;

            if (targetDiff.LengthSquared < 0.1) // close enough
            {
                // stop walking
                if (SelfEntity.TryGetComponent<AiControllerComponent>(out var mover))
                {
                    mover.VelocityDir = Vector2.Zero;
                }

                IdlePositiveEdge(ref rngState);
                return;
            }

            // continue walking
            if (SelfEntity.TryGetComponent<AiControllerComponent>(out var moverTwo))
            {
                moverTwo.VelocityDir = targetDiff.Normalized;
            }
        }

        private void DisabledPositiveEdge()
        {
            _startStateTime = _timeMan.CurTime;
            _CurrentState = FsmState.Disabled;
        }

        private void DisabledState()
        {
            if(_timeMan.CurTime < _startStateTime + DisabledTimeout)
                return;

            if (ActionBlockerSystem.CanMove(SelfEntity))
            {
                var rngState = GenSeed();
                IdlePositiveEdge(ref rngState);
            }
            else
                DisabledPositiveEdge();
        }

        private void EmitProfanity(ref uint rngState)
        {
            if(Random01(ref rngState) < 0.5f)
                return;

            var pick = (int) Math.Round(Random01(ref rngState) * (_normalAssistantConversation.Count - 1));
            _chatMan.EntitySay(SelfEntity, _normalAssistantConversation[pick]);
        }

        private uint GenSeed()
        {
            return RotateRight((uint)_timeMan.CurTick.GetHashCode(), 11) ^ (uint)SelfEntity.Uid.GetHashCode();
        }

        private uint RotateRight(uint n, int s)
        {
            return (n << (32 - s)) | (n >> s);
        }

        private float Random01(ref uint state)
        {
            DebugTools.Assert(state != 0);

            //xorshift32
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            return state / (float)uint.MaxValue;
        }

        private enum FsmState
        {
            None,
            Idle,
            Walking,
            Disabled
        }
    }
}
