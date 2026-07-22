// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.IO;
using Content.Shared.Roles;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.DeadSpace.Preferences;

public sealed class MsgUpdateAntagFavorites : NetMessage
{
    private const int MaxFavorites = 256;

    public override MsgGroups MsgGroup => MsgGroups.Command;
    public List<ProtoId<AntagPrototype>> Favorites = [];

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Favorites.Clear();
        var length = buffer.ReadVariableInt32();
        if (length is < 0 or > MaxFavorites)
            throw new InvalidDataException($"Invalid favorite antag count: {length}");

        for (var i = 0; i < length; i++)
            Favorites.Add(buffer.ReadString());
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.WriteVariableInt32(Favorites.Count);
        foreach (var favorite in Favorites)
            buffer.Write(favorite.Id);
    }
}
