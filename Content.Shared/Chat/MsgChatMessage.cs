using System.IO;
using Content.Shared.Chat.Prototypes;
using JetBrains.Annotations;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Chat
{
    [Serializable, NetSerializable]
    public sealed class ChatMessage
    {
        /// <summary>
        /// Which channel this message is being sent on.
        /// </summary>
        public ProtoId<CommunicationChannelPrototype> CommunicationChannel;

        /// <summary>
        /// This is the text spoken by the entity, after accents and such were applied.
        /// This should have <see cref="FormattedMessage.EscapeText"/> applied before using it in any rich text box.
        /// </summary>
        public FormattedMessage Message;

        /// <summary>
        /// The entity responsible for sending the message, if there is one.
        /// </summary>
        public NetEntity SenderEntity;

        /// <summary>
        ///     Identifier sent when <see cref="SenderEntity"/> is <see cref="NetEntity.Invalid"/>
        ///     if this was sent by a player to assign a key to the sender of this message.
        ///     This is unique per sender.
        /// </summary>
        public int? SenderKey;

        /// <summary>
        /// If true, this message should not display in chat.
        /// </summary>
        public bool HideChat;

        [NonSerialized]
        public bool Read;

        public ChatMessage(ProtoId<CommunicationChannelPrototype> communicationChannel, FormattedMessage message, NetEntity source, int? senderKey, bool hideChat = false)
        {
            CommunicationChannel = communicationChannel;
            Message = message;
            SenderEntity = source;
            SenderKey = senderKey;
            HideChat = hideChat;
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
            using var stream = new MemoryStream(length);
            buffer.ReadAlignedMemory(stream, length);
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
