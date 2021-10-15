using System.Collections.Generic;
using System.Diagnostics;
using Lidgren.Network;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;

namespace Content.Shared.Administration.Menu
{
    public class AdminMenuPlayerListMessage : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.Command;

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
                var uid = buffer.ReadEntityUid();

                PlayersInfo.Add(new PlayerInfo(username, characterName, antag, uid));
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
                buffer.Write(player.EntityUid);
            }
        }

        public record PlayerInfo(string Username, string CharacterName, bool Antag, EntityUid EntityUid);
    }
}
