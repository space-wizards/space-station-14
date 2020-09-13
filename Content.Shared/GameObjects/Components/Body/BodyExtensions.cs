#nullable enable
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Shared.GameObjects.Components.Body
{
    public static class BodyExtensions
    {
        public static IBody? GetBodyShared(this IEntity entity)
        {
            if (!entity.TryGetComponent(out IHasBody? hasBody))
            {
                return null;
            }

            return hasBody.Body;
        }

        public static bool TryGetBodyShared(this IEntity entity, [NotNullWhen(true)] out IBody? body)
        {
            return (body = entity.GetBodyShared()) != null;
        }
    }
}
