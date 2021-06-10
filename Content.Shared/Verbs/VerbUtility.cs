#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Content.Shared.Verbs
{
    public static class VerbUtility
    {
        public const float InteractionRange = 2;
        public const float InteractionRangeSquared = InteractionRange * InteractionRange;

        // TODO: This is a quick hack. Verb objects should absolutely be cached properly.
        // This works for now though.
        public static IEnumerable<(IComponent, Verb)> GetVerbs(IEntity entity)
        {
            var typeFactory = IoCManager.Resolve<IDynamicTypeFactory>();

            foreach (var component in entity.GetAllComponents())
            {
                var type = component.GetType();
                foreach (var nestedType in type.GetAllNestedTypes())
                {
                    if (!typeof(Verb).IsAssignableFrom(nestedType) || nestedType.IsAbstract)
                    {
                        continue;
                    }

                    var verb = typeFactory.CreateInstance<Verb>(nestedType);
                    yield return (component, verb);
                }
            }
        }

        /// <summary>
        /// Returns an IEnumerable of all classes inheriting <see cref="GlobalVerb"/> with the <see cref="GlobalVerbAttribute"/> attribute.
        /// </summary>
        /// <param name="assembly">The assembly to search for global verbs in.</param>
        public static IEnumerable<GlobalVerb> GetGlobalVerbs(Assembly assembly)
        {
            var typeFactory = IoCManager.Resolve<IDynamicTypeFactory>();

            foreach (Type type in assembly.GetTypes())
            {
                if (Attribute.IsDefined(type, typeof(GlobalVerbAttribute)))
                {
                    if (!typeof(GlobalVerb).IsAssignableFrom(type) || type.IsAbstract)
                    {
                        continue;
                    }
                    yield return typeFactory.CreateInstance<GlobalVerb>(type);
                }
            }
        }

        public static bool VerbAccessChecks(IEntity user, IEntity target, VerbBase verb)
        {
            if (verb.RequireInteractionRange && !InVerbUseRange(user, target))
            {
                return false;
            }

            if (verb.BlockedByContainers && !VerbContainerCheck(user, target))
            {
                return false;
            }

            return true;
        }

        public static bool InVerbUseRange(IEntity user, IEntity target)
        {
            var distanceSquared = (user.Transform.WorldPosition - target.Transform.WorldPosition)
                .LengthSquared;
            if (distanceSquared > InteractionRangeSquared)
            {
                return false;
            }
            return true;
        }

        public static bool VerbContainerCheck(IEntity user, IEntity target)
        {
            if (!user.IsInSameOrNoContainer(target))
            {
                if (!target.TryGetContainer(out var container) ||
                    container.Owner != user)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
