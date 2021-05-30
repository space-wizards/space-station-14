using System.Collections.Generic;

namespace Content.Server.Holiday.Interfaces
{
    public interface IHolidayManager
    {
        void Initialize();
        void RefreshCurrentHolidays();
        void DoGreet();
        void DoCelebrate();
        IEnumerable<HolidayPrototype> GetCurrentHolidays();
        bool IsCurrentlyHoliday(string holiday);
    }
}
