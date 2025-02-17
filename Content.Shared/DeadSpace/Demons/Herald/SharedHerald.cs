// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared.DeadSpace.Demons.Herald;

public sealed partial class HeraldSpawnActionEvent : InstantActionEvent
{

}

public sealed partial class HeraldEnrageActionEvent : InstantActionEvent
{

}

[NetSerializable, Serializable]
public enum HeraldVisuals : byte
{
    Herald,
    Enraged
}
