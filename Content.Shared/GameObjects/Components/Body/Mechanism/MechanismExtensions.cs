#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.GameObjects.Components.Body.Behavior;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Shared.GameObjects.Components.Body.Mechanism
{
    public static class MechanismExtensions
    {
        public static bool HasMechanismBehavior<T>(this IEntity entity) where T : IMechanismBehavior
        {
            // TODO BODY optimize
            return entity.TryGetBody(out var body) &&
                   body.Parts.Values.Any(p => p.Mechanisms.Any(m => m.Owner.HasComponent<T>()));
        }

        public static IEnumerable<T> GetMechanismBehaviors<T>(this IEntity entity) where T : class, IMechanismBehavior
        {
            if (!entity.TryGetBody(out var body))
            {
                yield break;
            }

            foreach (var part in body.Parts.Values)
            foreach (var mechanism in part.Mechanisms)
            {
                if (mechanism.Owner.TryGetComponent(out T? behavior))
                {
                    yield return behavior;
                }
            }
        }

        public static bool TryGetMechanismBehaviors<T>(this IEntity entity, [NotNullWhen(true)] out List<T>? behaviors)
            where T : class, IMechanismBehavior
        {
            behaviors = entity.GetMechanismBehaviors<T>().ToList();

            if (behaviors.Count == 0)
            {
                behaviors = null;
                return false;
            }

            return true;
        }
    }
}
