using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Server.Interfaces.Chat;
using JetBrains.Annotations;
using Robust.Server.AI;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Content.Server.AI
{
    /// <summary>
    /// Designed for a a stationary entity that regularly advertises things (vending machine).
    /// </summary>
    [AiLogicProcessor("StaticBarker")]
    class StaticBarkerProcessor : AiLogicProcessor
    {
        [Dependency, UsedImplicitly] private readonly IGameTiming _timeMan;
        [Dependency, UsedImplicitly] private readonly IChatManager _chatMan;

        private static readonly TimeSpan MinimumDelay = TimeSpan.FromSeconds(15);

        private TimeSpan _nextBark;

        public override void Update(float frameTime)
        {
            if(_timeMan.CurTime < _nextBark)
                return;

            _chatMan.EntitySay(SelfEntity, $"Time: {_timeMan.CurTime.ToString()}");

            var rngState = GenSeed();
            _nextBark = MinimumDelay + TimeSpan.FromSeconds(Random01(ref rngState) * 10);
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
    }
}
