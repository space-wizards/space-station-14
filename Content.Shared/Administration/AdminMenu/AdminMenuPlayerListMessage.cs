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

        public List<PlayerInfo> PlayersInfo = new();

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            var count = buffer.ReadInt32();

            PlayersInfo.Clear();

            for (var i = 0; i < count; i++)
            {
                var username = buffer.ReadString();
                var characterName = buffer.ReadString();
                var antag = buffer.ReadBoolean();

                PlayersInfo.Add(new PlayerInfo(username, characterName, antag));
            }
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(PlayersInfo.Count);

            foreach (var player in PlayersInfo)
            {
                buffer.Write(player.Username);
                buffer.Write(player.CharacterName);
                buffer.Write(player.Antag);
            }
        }

        public record PlayerInfo(string Username, string CharacterName, bool Antag);
    }
}
