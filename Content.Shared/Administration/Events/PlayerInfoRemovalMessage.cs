using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration.Events
{
    [NetSerializable, Serializable]
    public class PlayerInfoRemovalMessage : EntityEventArgs
    {
        public NetUserId NetUserId;
    }
}
