using System.Text.Json;
using Robust.Shared.Player;

namespace Content.Server.Administration.Logs.Converters;

[AdminLogConverter]
public sealed class EntityUidConverter : AdminLogConverter<EntityUid>
{
    // System.Text.Json actually keeps hold of your JsonSerializerOption instances in a cache on .NET 7.
    // Use a weak reference to avoid holding server instances live too long in integration tests.
    private WeakReference<IEntityManager> _entityManager = default!;

    public override void Init(IDependencyCollection dependencies)
    {
        _entityManager = new WeakReference<IEntityManager>(dependencies.Resolve<IEntityManager>());
    }

    public static void Write(Utf8JsonWriter writer, EntityUid value, JsonSerializerOptions options, IEntityManager entities)
    {
        writer.WriteStartObject();

        writer.WriteNumber("id", (int) value);

        if (entities.TryGetComponent(value, out MetaDataComponent? metaData))
        {
            writer.WriteString("name", metaData.EntityName);
        }

        if (entities.TryGetComponent(value, out ActorComponent? actor))
        {
            writer.WriteString("player", actor.PlayerSession.UserId.UserId);
        }

        writer.WriteEndObject();
    }

    public override void Write(Utf8JsonWriter writer, EntityUid value, JsonSerializerOptions options)
    {
        if (!_entityManager.TryGetTarget(out var entityManager))
            throw new InvalidOperationException("EntityManager got garbage collected!");

        Write(writer, value, options, entityManager);
    }
}
