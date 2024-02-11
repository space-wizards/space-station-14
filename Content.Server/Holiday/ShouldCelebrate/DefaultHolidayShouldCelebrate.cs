using Content.Server.Holiday.Interfaces;

namespace Content.Server.Holiday.ShouldCelebrate
{
    [Virtual, DataDefinition]
    public partial class DefaultHolidayShouldCelebrate : IHolidayShouldCelebrate
    {
        public virtual bool ShouldCelebrate(DateTime date, HolidayPrototype holiday)
        {
            if (holiday.EndDay == 0)
                holiday.EndDay = holiday.BeginDay;

            if (holiday.EndMonth == Month.Invalid)
                holiday.EndMonth = holiday.BeginMonth;

            // Holiday spans multiple months in one year.
            if(holiday.EndMonth > holiday.BeginMonth)
            {
                // In final month.
                if (date.Month == (int) holiday.EndMonth && date.Day <= holiday.EndDay)
                    return true;

                // In first month.
                if (date.Month == (int) holiday.BeginMonth && date.Day >= holiday.BeginDay)
                    return true;

                // Holiday spans more than 2 months, and we're in the middle.
                if (date.Month > (int) holiday.BeginMonth && date.Month < (int) holiday.EndMonth)
                    return true;
            }

            // Holiday starts and stops in the same month.
            else if (holiday.EndMonth == holiday.BeginMonth)
            {
                if (date.Month == (int) holiday.BeginMonth && date.Day >= holiday.BeginDay && date.Day <= holiday.EndDay)
                    return true;
            }

            // Holiday starts in one year and ends in the next.
            else
            {
                // Holiday ends next year.
                if (date.Month >= (int) holiday.BeginMonth && date.Day >= holiday.BeginDay)
                    return true;

                // Holiday started last year.
                if (date.Month <= (int) holiday.EndMonth && date.Day <= holiday.EndDay)
                    return true;
            }

            return false;
        }
    }
}
