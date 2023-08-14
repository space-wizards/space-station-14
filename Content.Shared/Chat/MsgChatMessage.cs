using JetBrains.Annotations;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using System.IO;

namespace Content.Shared.Chat
{
    [Serializable, NetSerializable]
    public sealed class ChatMessage
    {
        public ChatChannel Channel;
        public string Message;
        public string WrappedMessage;
        public NetEntity SenderEntity;
        public bool HideChat;
        public Color? MessageColorOverride;
        public string? AudioPath;
        public float AudioVolume;

        [NonSerialized]
        public bool Read;

        public ChatMessage(ChatChannel channel, string message, string wrappedMessage, NetEntity source, bool hideChat = false, Color? colorOverride = null, string? audioPath = null, float audioVolume = 0)
        {
            Channel = channel;
            Message = message;
            WrappedMessage = wrappedMessage;
            SenderEntity = source;
            HideChat = hideChat;
            MessageColorOverride = colorOverride;
            AudioPath = audioPath;
            AudioVolume = audioVolume;
        }
    }

    /// <summary>
    ///     Sent from server to client to notify the client about a new chat message.
    /// </summary>
    [UsedImplicitly]
    public sealed class MsgChatMessage : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.Command;

        public ChatMessage Message = default!;

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
        {
            var length = buffer.ReadVariableInt32();
            using var stream = buffer.ReadAlignedMemory(length);
            serializer.DeserializeDirect(stream, out Message);
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
        {
            var stream = new MemoryStream();
            serializer.SerializeDirect(stream, Message);
            buffer.WriteVariableInt32((int) stream.Length);
            buffer.Write(stream.AsSpan());
        }
    }
}
