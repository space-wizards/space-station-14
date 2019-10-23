using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Utility;

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

        /// <summary>
        ///     Gets the text string that will be shown to <paramref name="user"/> in the right click menu.
        /// </summary>
        /// <param name="user">The entity of the user opening this menu.</param>
        /// <param name="component">The component instance for which this verb is being loaded.</param>
        /// <returns>The text string that is shown in the right click menu for this verb.</returns>
        public abstract string GetText(IEntity user, IComponent component);

        /// <summary>
        ///     Gets the visibility level of this verb in the right click menu.
        /// </summary>
        /// <param name="user">The entity of the user opening this menu.</param>
        /// <param name="component">The component instance for which this verb is being loaded.</param>
        /// <returns>The visibility level of the verb in the client's right click menu.</returns>
        public abstract VerbVisibility GetVisibility(IEntity user, IComponent component);

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
        ///     Gets the visibility level of this verb in the right click menu.
        /// </summary>
        /// <param name="user">The entity of the user opening this menu.</param>
        /// <param name="component">The component instance for which this verb is being loaded.</param>
        /// <returns>The visibility level of the verb in the client's right click menu.</returns>
        protected abstract VerbVisibility GetVisibility(IEntity user, T component);

        /// <summary>
        ///     Invoked when this verb is activated from the right click menu.
        /// </summary>
        /// <param name="user">The entity of the user opening this menu.</param>
        /// <param name="component">The component instance for which this verb is being loaded.</param>
        protected abstract void Activate(IEntity user, T component);

        /// <inheritdoc />
        public sealed override string GetText(IEntity user, IComponent component)
        {
            return GetText(user, (T) component);
        }

        /// <inheritdoc />
        public sealed override VerbVisibility GetVisibility(IEntity user, IComponent component)
        {
            return GetVisibility(user, (T) component);
        }

        /// <inheritdoc />
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
    [MeansImplicitUse]
    public sealed class VerbAttribute : Attribute
    {
    }

    /// <summary>
    /// Possible states of visibility for the verb in the right click menu.
    /// </summary>
    public enum VerbVisibility
    {
        /// <summary>
        /// The verb will be listed in the right click menu.
        /// </summary>
        Visible,

        /// <summary>
        /// The verb will be listed, but it will be grayed out and unable to be clicked on.
        /// </summary>
        Disabled,

        /// <summary>
        /// The verb will not be listed in the right click menu.
        /// </summary>
        Invisible
    }
}
