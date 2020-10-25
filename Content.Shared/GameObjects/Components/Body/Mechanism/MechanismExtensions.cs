#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.GameObjects.Components.Body.Behavior;
using Content.Shared.GameObjects.Components.Body.Part;

namespace Content.Shared.GameObjects.Components.Body.Mechanism
{
    public static class MechanismExtensions
    {
        public static bool HasMechanismBehavior<T>(this IBody body)
        {
            return body.Parts.Values.Any(p => p.HasMechanismBehavior<T>());
        }

        public static bool HasMechanismBehavior<T>(this IBodyPart part)
        {
            return part.Mechanisms.Any(m => m.Owner.HasComponent<T>());
        }

        public static bool HasMechanismBehavior<T>(this IMechanism mechanism)
        {
            return mechanism.Owner.HasComponent<T>();
        }

        public static IEnumerable<IMechanismBehavior> GetMechanismBehaviors(this IBody body)
        {
            foreach (var part in body.Parts.Values)
            foreach (var mechanism in part.Mechanisms)
            foreach (var behavior in mechanism.Owner.GetAllComponents<IMechanismBehavior>())
            {
                yield return behavior;
            }
        }

        public static bool TryGetMechanismBehaviors(this IBody body,
            [NotNullWhen(true)] out List<IMechanismBehavior>? behaviors)
        {
            behaviors = body.GetMechanismBehaviors().ToList();

            if (behaviors.Count == 0)
            {
                behaviors = null;
                return false;
            }

            return true;
        }

        public static IEnumerable<T> GetMechanismBehaviors<T>(this IBody body) where T : class, IMechanismBehavior
        {
            foreach (var part in body.Parts.Values)
            foreach (var mechanism in part.Mechanisms)
            {
                if (mechanism.Owner.TryGetComponent(out T? behavior))
                {
                    yield return behavior;
                }
            }
        }

        public static bool TryGetMechanismBehaviors<T>(this IBody entity, [NotNullWhen(true)] out List<T>? behaviors)
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
