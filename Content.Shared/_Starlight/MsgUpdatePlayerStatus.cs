using Content.Shared.Administration;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Starlight;

public sealed class MsgUpdatePlayerStatus : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public PlayerData? Player;
    public string? DiscordLink;
    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        if (buffer.ReadBoolean())
        {
            buffer.ReadPadBits();
            var flags = (PlayerFlags)buffer.ReadUInt32();
            var balance = buffer.ReadInt32();
            var title = buffer.ReadString();
            var ghostTheme = buffer.ReadString();

            DiscordLink = buffer.ReadString();
            Player = new PlayerData
            {
                Title = title,
                GhostTheme = ghostTheme,
                Balance = balance,
                Flags = flags,
            };
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Player != null);

        if (Player == null) return;

        buffer.WritePadBits();
        buffer.Write((uint)Player.Flags);
        buffer.Write(Player.Balance);
        buffer.Write(Player.Title);
        buffer.Write(Player.GhostTheme);
        buffer.Write(DiscordLink);
    }

    public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableOrdered;
}
