#nullable enable
using System.Diagnostics.CodeAnalysis;
using Content.Shared.GameObjects.Components.Body.Part;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Shared.GameObjects.Components.Body
{
    public static class BodyExtensions
    {
        public static IBody? GetBodyShared(this IEntity entity)
        {
            return entity.GetComponentOrNull<IBody>();
        }

        public static bool TryGetBodyShared(this IEntity entity, [NotNullWhen(true)] out IBody? body)
        {
            return (body = entity.GetBodyShared()) != null;
        }
    }
}
