#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.GameObjects.Components.Body.Part.Property;

namespace Content.Shared.GameObjects.Components.Body.Part
{
    public static class BodyPartExtensions
    {
        public static bool HasProperty(this IBodyPart part, Type type)
        {
            return part.Owner.HasComponent(type);
        }

        public static bool HasProperty<T>(this IBodyPart part) where T : class, IBodyPartProperty
        {
            return part.HasProperty(typeof(T));
        }

        public static bool TryGetProperty(this IBodyPart part, Type type,
            [NotNullWhen(true)] out IBodyPartProperty? property)
        {
            if (!part.Owner.TryGetComponent(type, out var component))
            {
                property = null;
                return false;
            }

            return (property = component as IBodyPartProperty) != null;
        }

        public static bool TryGetProperty<T>(this IBodyPart part, [NotNullWhen(true)] out T? property) where T : class, IBodyPartProperty
        {
            return part.Owner.TryGetComponent(out property);
        }
    }
}
