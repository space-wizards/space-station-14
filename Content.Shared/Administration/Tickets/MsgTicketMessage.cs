using Content.Shared.Chat;
using JetBrains.Annotations;
using Lidgren.Network;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;

namespace Content.Shared.Administration
{
    [UsedImplicitly]
    public class MsgTicketMessage : NetMessage
    {
        #region REQUIRED

        public const MsgGroups GROUP = MsgGroups.Command;
        public const string NAME = nameof(MsgTicketMessage);
        public MsgTicketMessage(INetChannel channel) : base(NAME, GROUP) { }

        #endregion

        public int Id { get; set; }
        public string Message { get; set; } = string.Empty;

        public NetUserId TargetPlayer { get; set; }

        public NetUserId? ClaimedAdmin { get; set; }

        public TicketAction Action { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            /*Channel = (ChatChannel) buffer.ReadInt16();
            Message = buffer.ReadString();
            MessageWrap = buffer.ReadString();

            switch (Channel)
            {
                case ChatChannel.Local:
                case ChatChannel.Dead:
                case ChatChannel.AdminChat:
                case ChatChannel.Emotes:
                    SenderEntity = buffer.ReadEntityUid();
                    break;
            }*/
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            /*buffer.Write((short)Channel);
            buffer.Write(Message);
            buffer.Write(MessageWrap);

            switch (Channel)
            {
                case ChatChannel.Local:
                case ChatChannel.Dead:
                case ChatChannel.AdminChat:
                case ChatChannel.Emotes:
                    buffer.Write(SenderEntity);
                    break;
            }*/
        }
    }
}
