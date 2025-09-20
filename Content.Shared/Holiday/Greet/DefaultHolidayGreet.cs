using Content.Shared.Holiday.Interfaces;

namespace Content.Shared.Holiday.Greet
{
    /// <summary>
    ///     Default greeting used by <see cref="HolidayPrototype"/>.
    /// </summary>
    [DataDefinition]
    public sealed partial class DefaultHolidayGreet : IHolidayGreet
    {
        public string Greet(HolidayPrototype holiday)
        {
            var holidayName = Loc.GetString(holiday.Name);
            return Loc.GetString("holiday-greet", ("holidayName", holidayName));
        }
    }
}
