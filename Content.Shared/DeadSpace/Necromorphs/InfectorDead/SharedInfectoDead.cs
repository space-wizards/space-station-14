// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DoAfter;
using Robust.Shared.Serialization;
using Content.Shared.Actions;

namespace Content.Shared.DeadSpace.Necromorphs.InfectorDead;

public sealed partial class InfectionNecroActionEvent : EntityTargetActionEvent
{

}

[Serializable, NetSerializable]
public sealed partial class InfectorDeadDoAfterEvent : SimpleDoAfterEvent
{

}
