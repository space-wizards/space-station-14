// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.FixedPoint;

namespace Content.Server.DeadSpace.Hooligan.Objectives; 

/// <summary>
/// Хранит количество употреблённых наркотиков
/// </summary>
[RegisterComponent, Access(typeof(HooliganDrugConditionSystem))]
public sealed partial class  HooliganDrugConditionComponent : Component
{
    public  FixedPoint2 Amount = FixedPoint2.Zero;
}
