using Content.Shared.Holiday.Interfaces;
using JetBrains.Annotations;

namespace Content.Shared.Holiday.Greet
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class Custom : IHolidayGreet
    {
        [DataField("text")] private string _greet = string.Empty;

        public string Greet(HolidayPrototype holiday)
        {
            return Loc.GetString(_greet);
        }
    }
}
