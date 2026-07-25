// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

namespace Content.Server.DeadSpace.Hooligan.Objectives;

/// <summary>
/// Stores every Hooligan objective tracking damage against this body and its assigned attacker body.
/// </summary>
[RegisterComponent, Access(typeof(HooliganBeatConditionSystem))]
public sealed partial class HooliganBeatTargetComponent : Component
{
    public readonly Dictionary<EntityUid, EntityUid> AttackersByObjective = new();
}
