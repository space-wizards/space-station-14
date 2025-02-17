// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Actions;
using Robust.Shared.Serialization;
using Content.Shared.DoAfter;

namespace Content.Shared.DeadSpace.Necromorphs.CorpseCollector;

public sealed partial class AbsorptionDeadNecroActionEvent : EntityTargetActionEvent
{

}

public sealed partial class SpawnPointNecroActionEvent : InstantActionEvent
{

}

public sealed partial class SpawnLeviathanActionEvent : InstantActionEvent
{

}

[Serializable, NetSerializable]
public sealed partial class AbsorptionDeadNecroDoAfterEvent : SimpleDoAfterEvent
{
}

[NetSerializable, Serializable]
public enum CorpseCollectorVisuals : byte
{
    lvl1,
    lvl2,
    lvl3
}
