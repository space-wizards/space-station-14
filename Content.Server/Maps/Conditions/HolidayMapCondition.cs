using System.Linq;
using Content.Server.Holiday;

namespace Content.Server.Maps.Conditions;

public sealed partial class HolidayMapCondition : GameMapCondition
{
    [DataField("holidays")]
    public string[] Holidays { get; private set; } = default!;

    public override bool Check(GameMapPrototype map)
    {
        var holidaySystem = IoCManager.Resolve<IEntityManager>().System<HolidaySystem>();

        return Holidays.Any(holiday => holidaySystem.IsCurrentlyHoliday(holiday)) ^ Inverted;
    }
}
