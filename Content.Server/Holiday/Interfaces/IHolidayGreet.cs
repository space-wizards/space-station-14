using Robust.Shared.Localization;

namespace Content.Server.Holiday.Interfaces
{
    public interface IHolidayGreet
    {
        string Greet(HolidayPrototype holiday);
    }
}
