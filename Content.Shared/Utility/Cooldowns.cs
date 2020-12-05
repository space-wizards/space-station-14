using System;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;

namespace Content.Shared.Utility
{
    /// <summary>
    /// Utilities for working with cooldowns.
    /// </summary>
    public static class Cooldowns
    {
        /// <returns>a cooldown interval starting
        /// at GameTiming.Curtime and ending at (offset) from CurTime.
        /// For example, passing TimeSpan.FromSeconds(5) will create an interval
        /// from now to 5 seconds from now.</returns>
        public static (TimeSpan start, TimeSpan end) FromNow(TimeSpan offset)
        {
            var now = IoCManager.Resolve<IGameTiming>().CurTime;
            return (now, now + offset);
        }

        /// <see cref="FromNow"/>
        public static (TimeSpan start, TimeSpan end) SecondsFromNow(double seconds)
        {
            return FromNow(TimeSpan.FromSeconds(seconds));
        }
    }
}
