using Content.Server.Holiday.Interfaces;
using Robust.Shared.Localization;

namespace Content.Server.Holiday.Greet
{
    public sealed class DefaultHolidayGreet : IHolidayGreet
    {
        public string Greet(HolidayPrototype holiday) => Loc.GetString("holiday-greet", ("holidayName", holiday.Name));
    }
}
