using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.NodeContainer
{
    public static class NodeVis
    {
        [Serializable, NetSerializable]
        public sealed class MsgEnable : EntityEventArgs
        {
            public MsgEnable(bool enabled)
            {
                Enabled = enabled;
            }

            public bool Enabled { get; }
        }

        [Serializable, NetSerializable]
        public sealed class MsgData : EntityEventArgs
        {

        }


    }
}
