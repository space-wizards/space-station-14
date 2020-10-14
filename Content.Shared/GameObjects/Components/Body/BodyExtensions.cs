#nullable enable
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Shared.GameObjects.Components.Body
{
    public static class BodyExtensions
    {
        public static T? GetBody<T>(this IEntity entity) where T : class, IBody
        {
            return entity.GetComponentOrNull<T>();
        }

        public static bool TryGetBody<T>(this IEntity entity, [NotNullWhen(true)] out T? body) where T : class, IBody
        {
            return (body = entity.GetBody<T>()) != null;
        }

        public static IBody? GetBody(this IEntity entity)
        {
            return entity.GetComponentOrNull<IBody>();
        }

        public static bool TryGetBody(this IEntity entity, [NotNullWhen(true)] out IBody? body)
        {
            return (body = entity.GetBody()) != null;
        }
    }
}
