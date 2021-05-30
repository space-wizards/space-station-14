#nullable enable
using System;
using Content.Shared.Administration;
using Lidgren.Network;
using Robust.Shared.Network;

namespace Content.Shared.Network.NetMessages
{
    public sealed class MsgUpdateAdminStatus : NetMessage
    {
        #region REQUIRED

        public const MsgGroups GROUP = MsgGroups.Command;
        public const string NAME = nameof(MsgUpdateAdminStatus);

        public MsgUpdateAdminStatus(INetChannel channel) : base(NAME, GROUP) { }

        #endregion

        public AdminData? Admin;
        public string[] AvailableCommands = Array.Empty<string>();

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            var count = buffer.ReadVariableInt32();

            AvailableCommands = new string[count];

            for (var i = 0; i < count; i++)
            {
                AvailableCommands[i] = buffer.ReadString();
            }

            if (buffer.ReadBoolean())
            {
                var active = buffer.ReadBoolean();
                buffer.ReadPadBits();
                var flags = (AdminFlags) buffer.ReadUInt32();
                var title = buffer.ReadString();

                Admin = new AdminData
                {
                    Active = active,
                    Title = title,
                    Flags = flags,
                };
            }

        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.WriteVariableInt32(AvailableCommands.Length);

            foreach (var cmd in AvailableCommands)
            {
                buffer.Write(cmd);
            }

            buffer.Write(Admin != null);

            if (Admin == null) return;

            buffer.Write(Admin.Active);
            buffer.WritePadBits();
            buffer.Write((uint) Admin.Flags);
            buffer.Write(Admin.Title);
        }

        public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableOrdered;
    }
}
