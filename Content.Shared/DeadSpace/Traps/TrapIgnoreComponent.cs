// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.NPC.Prototypes;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeadSpace.Traps;

/// <summary>
/// Defines entities that do not activate the trap.
/// The whitelist supports entity tags and components; factions are checked separately.
/// </summary>
[RegisterComponent]
public sealed partial class TrapIgnoreComponent : Component
{
    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public HashSet<ProtoId<NpcFactionPrototype>> Factions = new();
}
