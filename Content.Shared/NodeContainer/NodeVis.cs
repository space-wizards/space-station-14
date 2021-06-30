using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
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
            public List<GroupData> Groups = new();
            public List<int> GroupDeletions = new();
        }

        [Serializable, NetSerializable]
        public sealed class GroupData
        {
            public int NetId;
            public string GroupId = "";
            public Color Color;
            public NodeDatum[] Nodes = Array.Empty<NodeDatum>();
        }

        [Serializable, NetSerializable]
        public sealed class NodeDatum
        {
            public EntityUid Entity;
            public int NetId;
            public int[] Reachable = Array.Empty<int>();
            public string Name = "";
            public string Type = "";
        }
    }
}
