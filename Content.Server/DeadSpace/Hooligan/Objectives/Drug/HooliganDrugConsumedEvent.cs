// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.FixedPoint;

namespace Content.Server.DeadSpace.Hooligan.Objectives;

/// <summary>
/// Поднимается, когда кто то употребил наркотики
/// </summary>

[ByRefEvent]
public readonly record struct HooliganDrugConsumedEvent(EntityUid Body, FixedPoint2 Amount);
