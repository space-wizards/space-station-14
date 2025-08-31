using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared._Starlight.Combat.Ranged.Pierce;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Physics;
using Content.Shared.Starlight;
using Content.Shared.Starlight.Utility;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Reflect;
using Lidgren.Network;
using Robust.Shared.Audio;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Utility;

namespace Content.Shared._NullLink;
public sealed class MsgUpdatePlayerRoles : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public HashSet<ulong> Roles = [];
    public string? DiscordLink;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        DiscordLink = buffer.ReadString();

        var length = buffer.ReadVariableInt32();
        Roles.Clear();
        for (var i = 0; i < length; i++)
            Roles.Add(buffer.ReadUInt64());
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(DiscordLink);

        buffer.WriteVariableInt32(Roles.Count);
        foreach (var role in Roles)
            buffer.Write(role);
    }

    public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableOrdered;
}
