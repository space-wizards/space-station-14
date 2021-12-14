using JetBrains.Annotations;
using Lidgren.Network;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Network;

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
        ///     What to "wrap" the message contents with. Example is stuff like 'Joe says: "{0}"'
        /// </summary>
        public string MessageWrap { get; set; } = string.Empty;

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


        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            Channel = (ChatChannel) buffer.ReadInt16();
            Message = buffer.ReadString();
            MessageWrap = buffer.ReadString();

            switch (Channel)
            {
                case ChatChannel.Local:
                case ChatChannel.Dead:
                case ChatChannel.Admin:
                case ChatChannel.Emotes:
                    SenderEntity = buffer.ReadEntityUid();
                    break;
            }
            MessageColorOverride = buffer.ReadColor();
            HideChat = buffer.ReadBoolean();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write((short)Channel);
            buffer.Write(Message);
            buffer.Write(MessageWrap);

            switch (Channel)
            {
                case ChatChannel.Local:
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
