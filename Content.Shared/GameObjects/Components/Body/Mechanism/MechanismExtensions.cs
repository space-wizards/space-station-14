#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.GameObjects.Components.Body.Behavior;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Shared.GameObjects.Components.Body.Mechanism
{
    public static class MechanismExtensions
    {
        public static bool TryGetMechanismBehaviors<T>(this IEntity entity, [NotNullWhen(true)] out List<T>? behaviors)
            where T : class, IMechanismBehavior
        {
            if (!entity.TryGetBodyShared(out var body))
            {
                behaviors = null;
                return false;
            }

            behaviors = new List<T>();

            foreach (var part in body.Parts.Values)
            foreach (var mechanism in part.Mechanisms)
            {
                if (mechanism.Owner.TryGetComponent(out T? behavior))
                {
                    behaviors.Add(behavior);
                }
            }

            return true;
        }
    }
}
