using System;

namespace Content.Server.Holiday.Interfaces
{
    public interface IHolidayShouldCelebrate
    {
        bool ShouldCelebrate(DateTime date, HolidayPrototype holiday);
    }
}
