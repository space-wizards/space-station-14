using System.Linq;
using Content.Server.Holiday;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.Maps.Conditions;

public class HolidayMapCondition : GameMapCondition
{
    [DataField("holidays")]
    public string[] Holidays { get; } = default!;

    public override bool Check(GameMapPrototype map)
    {
        var holidaySystem = EntitySystem.Get<HolidaySystem>();

        return Holidays.Any(holiday => holidaySystem.IsCurrentlyHoliday(holiday)) ^ Inverted;
    }
}
