// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.FixedPoint;
using Content.Shared.Mind.Filters;

namespace Content.Server.DeadSpace.Hooligan.Objectives; 

/// <summary>
/// Условие цели Хулигана: накопить нужное количество урона по жертве. 
/// Хранит уже нанесённый урон.
/// </summary>
[RegisterComponent, Access(typeof(HooliganFightConditionSystem))]
public sealed partial class  HooliganFightConditionComponent : Component
{
    public  FixedPoint2 DamageDealt = FixedPoint2.Zero;

    [DataField]
    public List<MindFilter> Filters = new(); // Поле для фильтра ролей
}
