#nullable enable
using System;
using Content.Shared.GameObjects.Verbs;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.GameObjects.EntitySystemMessages
{
    public static class BwoinkSystemMessages
    {
        [Serializable, NetSerializable]
        public class BwoinkTextMessage : EntitySystemMessage
        {
            public NetUserId ChannelId { get; }
            public string Text { get; }

            public BwoinkTextMessage(NetUserId channelId, string text)
            {
                ChannelId = channelId;
                Text = text;
            }
        }
    }
}
