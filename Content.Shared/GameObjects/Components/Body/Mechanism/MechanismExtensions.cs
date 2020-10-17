#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.GameObjects.Components.Body.Behavior;
using Content.Shared.GameObjects.Components.Body.Part;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Shared.GameObjects.Components.Body.Mechanism
{
    public static class MechanismExtensions
    {
        public static bool HasMechanismBehavior<T>(this IEntity entity) where T : IMechanismBehavior
        {
            // TODO BODY optimize
            if (entity.TryGetComponent(out IBody? body))
            {
                return body.HasMechanismBehavior<T>();
            }

            if (entity.TryGetComponent(out IBodyPart? part))
            {
                return part.HasMechanismBehavior<T>();
            }

            if (entity.TryGetComponent(out IMechanism? mechanism))
            {
                return mechanism.HasMechanismBehavior<T>();
            }

            return false;
        }

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

        public static IEnumerable<IMechanismBehavior> GetMechanismBehaviors(this IEntity entity)
        {
            if (!entity.TryGetComponent(out IBody? body))
            {
                yield break;
            }

            foreach (var part in body.Parts.Values)
            foreach (var mechanism in part.Mechanisms)
            foreach (var behavior in mechanism.Owner.GetAllComponents<IMechanismBehavior>())
            {
                yield return behavior;
            }
        }

        public static bool TryGetMechanismBehaviors(this IEntity entity,
            [NotNullWhen(true)] out List<IMechanismBehavior>? behaviors)
        {
            behaviors = entity.GetMechanismBehaviors().ToList();

            if (behaviors.Count == 0)
            {
                behaviors = null;
                return false;
            }

            return true;
        }

        public static IEnumerable<IMechanismBehavior> GetMechanismBehaviors(this IEntity entity)
        {
            if (!entity.TryGetBody(out var body))
            {
                yield break;
            }

            foreach (var part in body.Parts.Values)
            foreach (var mechanism in part.Mechanisms)
            foreach (var behavior in mechanism.Owner.GetAllComponents<IMechanismBehavior>())
            {
                yield return behavior;
            }
        }

        public static bool TryGetMechanismBehaviors(this IEntity entity,
            [NotNullWhen(true)] out List<IMechanismBehavior>? behaviors)
        {
            behaviors = entity.GetMechanismBehaviors().ToList();

            if (behaviors.Count == 0)
            {
                behaviors = null;
                return false;
            }

            return true;
        }

        public static IEnumerable<T> GetMechanismBehaviors<T>(this IEntity entity) where T : class, IMechanismBehavior
        {
            if (!entity.TryGetComponent(out IBody? body))
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
