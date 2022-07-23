using Robust.Shared.Timing;

namespace Content.Shared.Cooldown
{
    /// <summary>
    /// Utilities for working with cooldowns.
    /// </summary>
    public static class Cooldowns
    {
        /// <param name="gameTiming">game timing to use, otherwise will resolve using IoCManager.</param>
        /// <returns>a cooldown interval starting at GameTiming.Curtime and ending at (offset) from CurTime.
        /// For example, passing TimeSpan.FromSeconds(5) will create an interval
        /// from now to 5 seconds from now.</returns>
        public static (TimeSpan start, TimeSpan end) FromNow(TimeSpan offset, IGameTiming? gameTiming = null)
        {
            var now = (gameTiming ?? IoCManager.Resolve<IGameTiming>()).CurTime;
            return (now, now + offset);
        }

        /// <see cref="FromNow"/>
        public static (TimeSpan start, TimeSpan end) SecondsFromNow(double seconds, IGameTiming? gameTiming = null)
        {
            return FromNow(TimeSpan.FromSeconds(seconds), gameTiming);
        }
    }
}
