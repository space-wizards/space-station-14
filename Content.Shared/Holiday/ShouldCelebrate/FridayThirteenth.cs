using Content.Shared.Holiday.Interfaces;
using JetBrains.Annotations;

namespace Content.Shared.Holiday.ShouldCelebrate
{
    /// <summary>
    ///     For Friday the 13th. Spooky!
    /// </summary>
    [UsedImplicitly]
    public sealed partial class FridayThirteenth : IHolidayShouldCelebrate
    {
        public bool ShouldCelebrate(DateTime date, HolidayPrototype holiday)
        {
            return date.Day == 13 && date.DayOfWeek == DayOfWeek.Friday;
        }
    }
}
