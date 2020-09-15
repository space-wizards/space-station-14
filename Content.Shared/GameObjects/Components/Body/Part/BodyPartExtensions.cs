#nullable enable
using System.Diagnostics.CodeAnalysis;
using Content.Shared.GameObjects.Components.Body.Part.Property;

namespace Content.Shared.GameObjects.Components.Body.Part
{
    public static class BodyPartExtensions
    {
        public static bool HasProperty<T>(this IBodyPart part) where T : class, IBodyPartProperty
        {
            return part.Owner.HasComponent<T>();
        }

        public static bool TryGetProperty<T>(this IBodyPart part, [NotNullWhen(true)] out T? property) where T : class, IBodyPartProperty
        {
            return part.Owner.TryGetComponent(out property);
        }
    }
}
