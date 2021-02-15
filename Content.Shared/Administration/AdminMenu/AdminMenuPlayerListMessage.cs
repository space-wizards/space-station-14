#nullable enable
using System.Collections.Generic;
using Lidgren.Network;
using Robust.Shared.Network;

namespace Content.Shared.Administration.AdminMenu
{
    public class AdminMenuPlayerListMessage : NetMessage
    {
        #region REQUIRED
        public static readonly MsgGroups GROUP = MsgGroups.Command;
        public static readonly string NAME = nameof(AdminMenuPlayerListMessage);
        public AdminMenuPlayerListMessage(INetChannel channel) : base(NAME, GROUP) { }
        #endregion

        public Dictionary<string, string> NamesToPlayers = default!;

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            var pairs = buffer.ReadInt32();

            NamesToPlayers = new Dictionary<string, string>();

            for (var i = 0; i < pairs; i++)
            {
                var name = buffer.ReadString();
                var player = buffer.ReadString();

                NamesToPlayers.Add(name, player);
            }
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(NamesToPlayers.Count);

            foreach (var (name, player) in NamesToPlayers)
            {
                buffer.Write(name);
                buffer.Write(player);
            }
        }
    }
}
