using System.Globalization;
using Content.Shared.Holiday.Interfaces;

namespace Content.Shared.Holiday.ShouldCelebrate
{
    public sealed partial class ChineseNewYear : IHolidayShouldCelebrate
    {
        public bool ShouldCelebrate(DateTime date, HolidayPrototype holiday)
        {
            var chinese = new ChineseLunisolarCalendar();

            var chineseNewYear = chinese.ToDateTime(date.Year, 1, 1, 0, 0, 0, 0);

            return date.Day == chineseNewYear.Day && date.Month == chineseNewYear.Month;
        }
    }
}
