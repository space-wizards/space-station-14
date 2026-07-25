// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

namespace Content.Server.DeadSpace.Hooligan.Objectives;

/// <summary>
/// Поднимается, когда кто-то нарисовал граффити мелком
/// Несёт того, кто нарисовал
/// </summary>

[ByRefEvent]
public readonly record struct HooliganGraffitiDrawnEvent(EntityUid Drawer);