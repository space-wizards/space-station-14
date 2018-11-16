using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using SS14.Shared.Interfaces.GameObjects;

namespace Content.Shared.GameObjects
{
    [UsedImplicitly]
    public abstract class Verb
    {
        public virtual bool RequireInteractionRange => true;
        public const float InteractionRange = 2;
        public const float InteractionRangeSquared = InteractionRange * InteractionRange;

        public abstract string GetText(IEntity user, IComponent component);
        public abstract bool IsDisabled(IEntity user, IComponent component);
        public abstract void Activate(IEntity user, IComponent component);
    }

    public abstract class Verb<T> : Verb where T : IComponent
    {
        protected abstract string GetText(IEntity user, T component);
        protected abstract bool IsDisabled(IEntity user, T component);
        protected abstract void Activate(IEntity user, T component);

        public sealed override string GetText(IEntity user, IComponent component)
        {
            return GetText(user, (T) component);
        }

        public sealed override bool IsDisabled(IEntity user, IComponent component)
        {
            return IsDisabled(user, (T) component);
        }

        public sealed override void Activate(IEntity user, IComponent component)
        {
            Activate(user, (T) component);
        }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class VerbAttribute : Attribute
    {

    }

    public static class VerbUtility
    {
        // TODO: This is a quick hack. Verb objects should absolutely be cached properly.
        // This works for now though.
        public static IEnumerable<(IComponent, Verb)> GetVerbs(IEntity entity)
        {
            foreach (var component in entity.GetAllComponents())
            {
                var type = component.GetType();
                foreach (var nestedType in type.GetNestedTypes())
                {
                    if (!typeof(Verb).IsAssignableFrom(nestedType) || nestedType.IsAbstract)
                    {
                        continue;
                    }

                    var verb = (Verb)Activator.CreateInstance(nestedType);
                    yield return (component, verb);
                }
            }
        }
    }
}
