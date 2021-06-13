#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Body.Behavior;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;

namespace Content.Server.Body.Behavior
{
    public static class MechanismExtensions
    {
        public static bool HasMechanismBehavior<T>(this IBody body) where T : IMechanismBehavior
        {
            return body.Parts.Any(p => p.Key.HasMechanismBehavior<T>());
        }

        public static bool HasMechanismBehavior<T>(this IBodyPart part) where T : IMechanismBehavior
        {
            return part.Mechanisms.Any(m => m.HasBehavior<T>());
        }

        public static IEnumerable<IMechanismBehavior> GetMechanismBehaviors(this IBody body)
        {
            foreach (var (part, _) in body.Parts)
            foreach (var mechanism in part.Mechanisms)
            foreach (var behavior in mechanism.Behaviors.Values)
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
            foreach (var (part, _) in body.Parts)
            foreach (var mechanism in part.Mechanisms)
            foreach (var behavior in mechanism.Behaviors.Values)
            {
                if (behavior is T tBehavior)
                {
                    yield return tBehavior;
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
