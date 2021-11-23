using System.Text.Json;
using Robust.Server.Player;

namespace Content.Server.Administration.Logs.Converters;

[AdminLogConverter]
public class PlayerSessionConverter : AdminLogConverter<SerializablePlayer>
{
    public override void Write(Utf8JsonWriter writer, SerializablePlayer value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        if (value.Player.AttachedEntity != null)
        {
            writer.WriteNumber("id", (int) value.Player.AttachedEntity.Uid);
            writer.WriteString("name", value.Player.AttachedEntity.Name);
        }

        writer.WriteString("player", value.Player.UserId.UserId);

        writer.WriteEndObject();
    }
}

public readonly struct SerializablePlayer
{
    public readonly IPlayerSession Player;

    public SerializablePlayer(IPlayerSession player)
    {
        Player = player;
    }
}
