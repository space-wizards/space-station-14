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
#pragma warning disable 649
        [Dependency] private readonly IGameTiming _timeMan;
        [Dependency] private readonly IChatManager _chatMan;
#pragma warning restore 649

        private static readonly TimeSpan MinimumDelay = TimeSpan.FromSeconds(15);
        private TimeSpan _nextBark;


        private static List<string> slogans = new List<string>
        {
            "Come try my great products today!",
            "More value for the way you live.",
            "Quality you'd expect at prices you wouldn't.",
            "The right stuff. The right price.",
        };

        public override void Update(float frameTime)
        {
            if(_timeMan.CurTime < _nextBark)
                return;
            
            var rngState = GenSeed();
            _nextBark = _timeMan.CurTime + MinimumDelay + TimeSpan.FromSeconds(Random01(ref rngState) * 10);

            var pick = (int)Math.Round(Random01(ref rngState) * (slogans.Count - 1));
            _chatMan.EntitySay(SelfEntity, slogans[pick]);
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
