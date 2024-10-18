using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration;

public sealed class MsgUsernameBan : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

    // special values of ID: -1 [remove] clear all
    // add [true] remove [false], id, expression, message
    public MsgUsernameBanContent UsernameBan = new();

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        int id = buffer.ReadInt32();
        bool add = buffer.ReadBoolean();
        bool regex = buffer.ReadBoolean();
        bool extendToBan = buffer.ReadBoolean();
        buffer.ReadPadBits();
        string expression = buffer.ReadString();

        UsernameBan = new(id, add, regex, extendToBan, expression);
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(UsernameBan.Id);
        buffer.Write(UsernameBan.Add);
        buffer.Write(UsernameBan.Regex);
        buffer.Write(UsernameBan.ExtendToBan);
        buffer.WritePadBits();
        buffer.Write(UsernameBan.Expression);
    }
}

public readonly record struct MsgUsernameBanContent(
    int Id,
    bool Add,
    bool Regex,
    bool ExtendToBan,
    string Expression
);

public sealed class MsgFullUsernameBan : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Entity;

    public MsgFullUsernameBanContent FullUsernameBan = new();

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        long creationTicks = buffer.ReadInt64();                //  0
        int id = buffer.ReadInt32();                            // 40

        //bits
        bool regex = buffer.ReadBoolean();                      // 60
        bool extendToBan = buffer.ReadBoolean();                // 61
        bool retired = buffer.ReadBoolean();                    // 62

        bool hasRoundId = buffer.ReadBoolean();                 // 63
        bool hasRestrictingAdmin = buffer.ReadBoolean();        // 64
        bool hasRetiringAdmin = buffer.ReadBoolean();           // 65
        bool hasRetireTime = buffer.ReadBoolean();              // 66
        buffer.ReadPadBits();

        int? roundId = null;
        if (hasRoundId)
        {
            roundId = buffer.ReadInt32();
        }

        Guid? restrictingAdmin = null;
        if (hasRestrictingAdmin)
        {
            var data = buffer.ReadString();
            if (data.Length > 0)
            {
                restrictingAdmin = new(data);
            }
        }

        Guid? retiringAdmin = null;
        if (hasRetiringAdmin)
        {
            var data = buffer.ReadString();
            if (data.Length > 0)
            {
                retiringAdmin = new(data);
            }
        }

        DateTime? retireTime = null;
        if (hasRetireTime)
        {
            retireTime = new(buffer.ReadInt64());
        }

        string expression = buffer.ReadString();
        string message = buffer.ReadString();

        FullUsernameBan = new(
            new(creationTicks),
            id,

            regex,
            extendToBan,
            retired,

            roundId,
            restrictingAdmin,
            retiringAdmin,
            retireTime,

            expression,
            message
        );
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(FullUsernameBan.CreationTime.Ticks);       //  0
        buffer.Write(FullUsernameBan.Id);                       // 40

        // bits
        buffer.Write(FullUsernameBan.Regex);                    // 60
        buffer.Write(FullUsernameBan.ExtendToBan);              // 61
        buffer.Write(FullUsernameBan.Retired);                  // 62

        buffer.Write(FullUsernameBan.RoundId != null);          // 63
        buffer.Write(FullUsernameBan.RestrictingAdmin != null); // 64
        buffer.Write(FullUsernameBan.RetiringAdmin != null);    // 65
        buffer.Write(FullUsernameBan.RetireTime != null);       // 66
        buffer.WritePadBits();

        if (FullUsernameBan.RoundId != null)
        {
            buffer.Write(FullUsernameBan.RoundId ?? -1);
        }

        if (FullUsernameBan.RestrictingAdmin != null)
        {
            buffer.Write(FullUsernameBan.RestrictingAdmin?.ToString());
        }

        if (FullUsernameBan.RetiringAdmin != null)
        {
            buffer.Write(FullUsernameBan.RetiringAdmin?.ToString());
        }

        if (FullUsernameBan.RetireTime != null)
        {
            buffer.Write(FullUsernameBan.RetireTime?.Ticks ?? 0);
        }

        buffer.Write(FullUsernameBan.Expression);
        buffer.Write(FullUsernameBan.Message);
    }
}

public readonly record struct MsgFullUsernameBanContent(
    DateTime CreationTime,
    int Id,

    bool Regex,
    bool ExtendToBan,
    bool Retired,

    int? RoundId,
    Guid? RestrictingAdmin,
    Guid? RetiringAdmin,
    DateTime? RetireTime,

    string Expression,
    string Message
);
