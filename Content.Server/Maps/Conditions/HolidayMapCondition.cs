using System.Linq;
using Content.Server.Holiday;

namespace Content.Server.Maps.Conditions;

public sealed class HolidayMapCondition : GameMapCondition
{
    [DataField("holidays")]
    public string[] Holidays { get; } = default!;

    public override bool Check(GameMapPrototype map)
    {
        var holidaySystem = EntitySystem.Get<HolidaySystem>();

        return Holidays.Any(holiday => holidaySystem.IsCurrentlyHoliday(holiday)) ^ Inverted;
    }
}
