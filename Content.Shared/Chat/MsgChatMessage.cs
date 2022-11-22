using JetBrains.Annotations;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Chat
{
    /// <summary>
    ///     Sent from server to client to notify the client about a new chat message.
    /// </summary>
    [UsedImplicitly]
    public sealed class MsgChatMessage : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.Command;

        /// <summary>
        ///     The channel the message is on. This can also change whether certain params are used.
        /// </summary>
        public ChatChannel Channel { get; set; }

        /// <summary>
        ///     The actual message contents.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        ///     Modified message with some wrapping text. E.g. 'Joe says: "HELP!"'
        /// </summary>
        public string WrappedMessage { get; set; } = string.Empty;

        /// <summary>
        ///     The sending entity.
        ///     Only applies to <see cref="ChatChannel.Local"/>, <see cref="ChatChannel.Dead"/> and <see cref="ChatChannel.Emotes"/>.
        /// </summary>
        public EntityUid SenderEntity { get; set; }

        /// <summary>
        /// The override color of the message
        /// </summary>
        public Color MessageColorOverride { get; set; } = Color.Transparent;

        public bool HideChat { get; set; }


        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
        {
            Channel = (ChatChannel) buffer.ReadInt16();
            Message = buffer.ReadString();
            WrappedMessage = buffer.ReadString();

            switch (Channel)
            {
                case ChatChannel.Local:
                case ChatChannel.Whisper:
                case ChatChannel.Dead:
                case ChatChannel.Admin:
                case ChatChannel.Emotes:
                    SenderEntity = buffer.ReadEntityUid();
                    break;
            }
            MessageColorOverride = buffer.ReadColor();
            HideChat = buffer.ReadBoolean();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
        {
            buffer.Write((short)Channel);
            buffer.Write(Message);
            buffer.Write(WrappedMessage);

            switch (Channel)
            {
                case ChatChannel.Local:
                case ChatChannel.Whisper:
                case ChatChannel.Dead:
                case ChatChannel.Admin:
                case ChatChannel.Emotes:
                    buffer.Write(SenderEntity);
                    break;
            }
            buffer.Write(MessageColorOverride);
            buffer.Write(HideChat);
        }
    }
}
