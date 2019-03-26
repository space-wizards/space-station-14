using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using SS14.Shared.Interfaces.GameObjects;

namespace Content.Shared.GameObjects
{
    /// <summary>
    ///     A verb is an action in the right click menu of an entity.
    /// </summary>
    /// <remarks>
    ///     To add a verb to an entity, define it as a nested class inside the owning component,
    ///     and mark it with <see cref="VerbAttribute"/>
    /// </remarks>
    [UsedImplicitly]
    public abstract class Verb
    {
        /// <summary>
        ///     If true, this verb requires the user to be inside within
        ///     <see cref="InteractionRange"/> meters from the entity on which this verb resides.
        /// </summary>
        public virtual bool RequireInteractionRange => true;

        public const float InteractionRange = 2;
        public const float InteractionRangeSquared = InteractionRange * InteractionRange;

        /// <summary>
        ///     Gets the text string that will be shown to <paramref name="user"/> in the right click menu.
        /// </summary>
        /// <param name="user">The entity of the user opening this menu.</param>
        /// <param name="component">The component instance for which this verb is being loaded.</param>
        /// <returns>The text string that is shown in the right click menu for this verb.</returns>
        public abstract string GetText(IEntity user, IComponent component);

        /// <summary>
        ///     Gets whether this verb is "disabled" in the right click menu.
        ///     The verb is still visible in disabled state, but greyed out.
        /// </summary>
        /// <param name="user">The entity of the user opening this menu.</param>
        /// <param name="component">The component instance for which this verb is being loaded.</param>
        /// <returns>True if the verb is disabled, false otherwise.</returns>
        public abstract bool IsDisabled(IEntity user, IComponent component);

        /// <summary>
        ///     Invoked when this verb is activated from the right click menu.
        /// </summary>
        /// <param name="user">The entity of the user opening this menu.</param>
        /// <param name="component">The component instance for which this verb is being loaded.</param>
        public abstract void Activate(IEntity user, IComponent component);
    }

    /// <inheritdoc />
    /// <summary>
    ///     Sub class of <see cref="T:Content.Shared.GameObjects.Verb" /> that works on a specific type of component,
    ///     to reduce casting boiler plate for implementations.
    /// </summary>
    /// <typeparam name="T">The type of component that this verb will run on.</typeparam>
    public abstract class Verb<T> : Verb where T : IComponent
    {
        /// <summary>
        ///     Gets the text string that will be shown to <paramref name="user"/> in the right click menu.
        /// </summary>
        /// <param name="user">The entity of the user opening this menu.</param>
        /// <param name="component">The component instance for which this verb is being loaded.</param>
        /// <returns>The text string that is shown in the right click menu for this verb.</returns>
        protected abstract string GetText(IEntity user, T component);

        /// <summary>
        ///     Gets whether this verb is "disabled" in the right click menu.
        ///     The verb is still visible in disabled state, but greyed out.
        /// </summary>
        /// <param name="user">The entity of the user opening this menu.</param>
        /// <param name="component">The component instance for which this verb is being loaded.</param>
        /// <returns>True if the verb is disabled, false otherwise.</returns>
        protected abstract bool IsDisabled(IEntity user, T component);

        /// <summary>
        ///     Invoked when this verb is activated from the right click menu.
        /// </summary>
        /// <param name="user">The entity of the user opening this menu.</param>
        /// <param name="component">The component instance for which this verb is being loaded.</param>
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

    /// <summary>
    ///     This attribute should be used on <see cref="Verb"/> implementations nested inside component classes,
    ///     so that they're automatically detected.
    /// </summary>
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
            foreach (var component in entity.GetComponentInstances())
            {
                var type = component.GetType();
                foreach (var nestedType in type.GetNestedTypes())
                {
                    if (!typeof(Verb).IsAssignableFrom(nestedType) || nestedType.IsAbstract)
                    {
                        continue;
                    }

                    var verb = (Verb) Activator.CreateInstance(nestedType);
                    yield return (component, verb);
                }
            }
        }
    }
}
