#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.GameObjects.Components.Body.Part.Property;

namespace Content.Shared.GameObjects.Components.Body.Part
{
    public static class BodyPartExtensions
    {
        /// <summary>
        ///     Checks if the given <see cref="IBodyPart"/> has the specified property.
        /// </summary>
        /// <param name="part">The <see cref="IBodyPart"/> to check in.</param>
        /// <param name="type">
        ///     The type of <see cref="IBodyPartProperty"/> to check for.
        /// </param>
        /// <returns>true if found, false otherwise.</returns>
        public static bool HasProperty(this IBodyPart part, Type type)
        {
            return part.Owner.HasComponent(type);
        }

        /// <summary>
        ///     Checks if the given <see cref="IBodyPart"/> has the specified property.
        /// </summary>
        /// <param name="part">The <see cref="IBodyPart"/> to check in.</param>
        /// <typeparam name="T">
        ///     The type of <see cref="IBodyPartProperty"/> to check for.
        /// </typeparam>
        /// <returns>true if found, false otherwise.</returns>
        public static bool HasProperty<T>(this IBodyPart part) where T : class, IBodyPartProperty
        {
            return part.HasProperty(typeof(T));
        }

        /// <summary>
        ///     Tries to retrieve the <see cref="IBodyPartProperty"/> with the
        ///     specified type.
        /// </summary>
        /// <param name="part">The <see cref="IBodyPart"/> to search in.</param>
        /// <param name="type">
        ///     The type of <see cref="IBodyPartProperty"/> to search for.
        /// </param>
        /// <param name="property">
        ///     The property, if it was found. Null otherwise.
        /// </param>
        /// <returns>
        ///     true if a component with the specified type was found, false otherwise.
        /// </returns>
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

        /// <summary>
        ///     Tries to retrieve the <see cref="IBodyPartProperty"/> with the
        ///     specified type.
        /// </summary>
        /// <param name="part">The <see cref="IBodyPart"/> to search in.</param>
        /// <typeparam name="T">
        ///     The type of <see cref="IBodyPartProperty"/> to search for.
        /// </typeparam>
        /// <param name="property">
        ///     The property, if it was found. Null otherwise.
        /// </param>
        /// <returns>
        ///     true if a component with the specified type was found, false otherwise.
        /// </returns>
        public static bool TryGetProperty<T>(this IBodyPart part, [NotNullWhen(true)] out T? property) where T : class, IBodyPartProperty
        {
            return part.Owner.TryGetComponent(out property);
        }
    }
}
