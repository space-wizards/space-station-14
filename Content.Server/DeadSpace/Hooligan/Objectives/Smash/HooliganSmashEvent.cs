// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

namespace Content.Server.DeadSpace.Hooligan.Objectives;

/// <summary>
/// Поднимается, когда кто-то сломал стекло/стакан
/// Несёт того, кто и что сломал
/// </summary>

[ByRefEvent]
public readonly record struct HooliganSmashEvent(EntityUid Smasher, EntityUid Target);
