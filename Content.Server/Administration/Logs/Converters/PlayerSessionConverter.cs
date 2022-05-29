using System.Text.Json;
using Robust.Server.Player;

namespace Content.Server.Administration.Logs.Converters;

[AdminLogConverter]
public sealed class PlayerSessionConverter : AdminLogConverter<SerializablePlayer>
{
    public override void Write(Utf8JsonWriter writer, SerializablePlayer value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        if (value.Player.AttachedEntity is {Valid: true} playerEntity)
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();

            writer.WriteNumber("id", (int) value.Player.AttachedEntity);
            writer.WriteString("name", entityManager.GetComponent<MetaDataComponent>(playerEntity).EntityName);
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
