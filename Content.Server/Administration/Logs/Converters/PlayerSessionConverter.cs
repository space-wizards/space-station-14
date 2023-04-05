using System.Text.Json;
using Robust.Server.Player;

namespace Content.Server.Administration.Logs.Converters;

[AdminLogConverter]
public sealed class PlayerSessionConverter : AdminLogConverter<SerializablePlayer>
{
    // System.Text.Json actually keeps hold of your JsonSerializerOption instances in a cache on .NET 7.
    // Use a weak reference to avoid holding server instances live too long in integration tests.
    private WeakReference<IEntityManager> _entityManager = default!;

    public override void Init(IDependencyCollection dependencies)
    {
        _entityManager = new WeakReference<IEntityManager>(dependencies.Resolve<IEntityManager>());
    }

    public override void Write(Utf8JsonWriter writer, SerializablePlayer value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        if (value.Player.AttachedEntity is {Valid: true} playerEntity)
        {
            if (!_entityManager.TryGetTarget(out var entityManager))
                throw new InvalidOperationException("EntityManager got garbage collected!");

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
