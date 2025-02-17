// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.DeadSpace.Spiders.SpiderInfector;

public sealed partial class SpiderInfectorActionEvent : EntityTargetActionEvent
{

}

[Serializable, NetSerializable]
public sealed partial class SpiderInfectorDoAfterEvent : SimpleDoAfterEvent
{

}
