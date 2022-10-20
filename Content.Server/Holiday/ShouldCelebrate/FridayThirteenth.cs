using Content.Server.Holiday.Interfaces;
using JetBrains.Annotations;

namespace Content.Server.Holiday.ShouldCelebrate
{
    /// <summary>
    ///     For Friday the 13th. Spooky!
    /// </summary>
    [UsedImplicitly]
    public sealed class FridayThirteenth : IHolidayShouldCelebrate
    {
        public bool ShouldCelebrate(DateTime date, Holiday holiday)
        {
            return date.Day == 13 && date.DayOfWeek == DayOfWeek.Friday;
        }
    }
}
