using System.Text.Json;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Administration.Logs.Converters;

[AdminLogConverter]
public class EntityConverter : AdminLogConverter<IEntity>
{
    [Dependency] private readonly IEntityManager _entities = default!;

    public override void Write(Utf8JsonWriter writer, IEntity value, JsonSerializerOptions options)
    {
        EntityUidConverter.Write(writer, value, options, _entities);
    }
}
