using Lidgren.Network;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Network;

namespace Content.Shared.Chat
{
    /// <summary>
    /// This message is sent by the server to let clients know what is the chat's character limit for this server.
    /// It is first sent by the client as a request
    /// </summary>
    public sealed class ChatMaxMsgLengthMessage : NetMessage
    {
        #region REQUIRED

        public const MsgGroups GROUP = MsgGroups.Command;
        public const string NAME = nameof(ChatMaxMsgLengthMessage);
        public ChatMaxMsgLengthMessage(INetChannel channel) : base(NAME, GROUP) { }

        #endregion

        /// <summary>
        /// The max length a player-sent message can get
        /// </summary>
        public int MaxMessageLength { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            MaxMessageLength = buffer.ReadInt32();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(MaxMessageLength);
        }
    }
}
