using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration.Events
{
    [Serializable, NetSerializable]
    public class FullPlayerListEvent : EntityEventArgs
    {
        public List<PlayerInfo> PlayersInfo = new();
    }
}
