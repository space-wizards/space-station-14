using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Players;

/// <summary>
/// Sent server -> client to inform the client of their role bans.
/// </summary>
public sealed class MsgRoleBans : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

    public List<BanInfo> Bans = new();

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        var count = buffer.ReadVariableInt32();
        Bans.EnsureCapacity(count);

        for (var i = 0; i < count; i++)
        {
            var ban = new BanInfo
            {
                Role = buffer.ReadString(),
                Reason = buffer.ReadString(),
                ExpirationTime = buffer.ReadBoolean() ? new DateTime(buffer.ReadInt64()) : null
            };
            Bans.Add(ban);
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.WriteVariableInt32(Bans.Count);

        foreach (var ban in Bans)
        {
            buffer.Write(ban.Role);
            buffer.Write(ban.Reason);
            buffer.Write(ban.ExpirationTime.HasValue);
            if (ban.ExpirationTime.HasValue)
            {
                buffer.Write(ban.ExpirationTime.Value.Ticks);
            }
        }
    }
}

public class BanInfo
{
    public string? Role { get; set; }
    public string? Reason { get; set; }
    public DateTime? ExpirationTime { get; set; }
}
