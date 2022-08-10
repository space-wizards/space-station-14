using Content.Server.Holiday.Interfaces;

namespace Content.Server.Holiday.Greet
{
    public sealed class DefaultHolidayGreet : IHolidayGreet
    {
        public string Greet(HolidayPrototype holiday)
        {
            var holidayName = Loc.GetString(holiday.Name);
            return Loc.GetString("holiday-greet", ("holidayName", holidayName));
        }
    }
}
