using Content.Shared.DoAfter;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Crayon;

[Serializable, NetSerializable]
public sealed partial class MagicCrayonDoAfterEvent : SimpleDoAfterEvent
{
    public NetCoordinates ClickLocation;

    public MagicCrayonDoAfterEvent()
    {
    }

    public MagicCrayonDoAfterEvent(NetCoordinates clickLocation)
    {
        ClickLocation = clickLocation;
    }
}
