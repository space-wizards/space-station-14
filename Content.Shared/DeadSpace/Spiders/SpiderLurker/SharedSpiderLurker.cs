// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared.DeadSpace.Spiders.SpiderLurker;

public sealed partial class SpiderLurkerActionEvent : InstantActionEvent
{

}

[NetSerializable, Serializable]
public enum SpiderLurkerVisuals : byte
{
    state,
    hide
}
