// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.FixedPoint;

namespace Content.Server.DeadSpace.Hooligan.Objectives; 

/// <summary>
/// Хранит уже нанесённый урон для цели.
/// </summary>
[RegisterComponent, Access(typeof(HooliganBeatConditionSystem))]
public sealed partial class  HooliganBeatConditionComponent : Component
{
    public  FixedPoint2 DamageDealt = FixedPoint2.Zero;
}
