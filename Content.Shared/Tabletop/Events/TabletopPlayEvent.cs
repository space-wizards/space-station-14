using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Tabletop.Events
{
    [Serializable, NetSerializable]
    public class TabletopPlayEvent : EntityEventArgs
    {
    }
}
