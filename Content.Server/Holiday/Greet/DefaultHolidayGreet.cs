using Content.Server.Holiday.Interfaces;
using Robust.Shared.Localization;

namespace Content.Server.Holiday.Greet
{
    public class DefaultHolidayGreet : IHolidayGreet
    {
        public string Greet(HolidayPrototype holiday)
        {
            return Loc.GetString("Have a happy {0}!", holiday.Name);
        }
    }
}
