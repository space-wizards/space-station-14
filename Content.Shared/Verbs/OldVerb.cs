using System;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Shared.Verbs
{
    /// <summary>
    ///     A verb is an action in the right click menu of an entity.
    /// </summary>
    /// <remarks>
    ///     To add a verb to an entity, define it as a nested class inside the owning component,
    ///     and mark it with <see cref="VerbAttribute"/>
    /// </remarks>
    [UsedImplicitly]
    public abstract class OldVerb : VerbBase
    {
        /// <summary>
        ///     Gets the visible verb data for the user.
        /// </summary>
        /// <remarks>
        ///     Implementations should write into <paramref name="data"/> to return their data.
        /// </remarks>
        /// <param name="user">The entity of the user opening this menu.</param>
        /// <param name="component">The component instance for which this verb is being loaded.</param>
        /// <param name="data">The data that must be filled into.</param>
        protected abstract void GetData(IEntity user, IComponent component, OldVerbData data);

        /// <summary>
        ///     Invoked when this verb is activated from the right click menu.
        /// </summary>
        /// <param name="user">The entity of the user opening this menu.</param>
        /// <param name="component">The component instance for which this verb is being loaded.</param>
        public abstract void Activate(IEntity user, IComponent component);

        public OldVerbData GetData(IEntity user, IComponent component)
        {
            var data = new OldVerbData();
            GetData(user, component, data);
            return data;
        }
    }

    /// <inheritdoc />
    /// <summary>
    ///     Sub class of <see cref="T:Content.Shared.Verbs.Verb" /> that works on a specific type of component,
    ///     to reduce casting boiler plate for implementations.
    /// </summary>
    /// <typeparam name="T">The type of component that this verb will run on.</typeparam>
    public abstract class Verb<T> : OldVerb where T : IComponent
    {
        /// <summary>
        ///     Gets the visible verb data for the user.
        /// </summary>
        /// <remarks>
        ///     Implementations should write into <paramref name="data"/> to return their data.
        /// </remarks>
        /// <param name="user">The entity of the user opening this menu.</param>
        /// <param name="component">The component instance for which this verb is being loaded.</param>
        /// <param name="data">The data that must be filled into.</param>
        protected abstract void GetData(IEntity user, T component, OldVerbData data);

        /// <summary>
        ///     Invoked when this verb is activated from the right click menu.
        /// </summary>
        /// <param name="user">The entity of the user opening this menu.</param>
        /// <param name="component">The component instance for which this verb is being loaded.</param>
        protected abstract void Activate(IEntity user, T component);

        protected sealed override void GetData(IEntity user, IComponent component, OldVerbData data)
        {
            GetData(user, (T) component, data);
        }

        /// <inheritdoc />
        public sealed override void Activate(IEntity user, IComponent component)
        {
            Activate(user, (T) component);
        }
    }

    /// <summary>
    ///     This attribute should be used on <see cref="OldVerb"/> implementations nested inside component classes,
    ///     so that they're automatically detected.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    [MeansImplicitUse]
    public sealed class VerbAttribute : Attribute
    {
    }
}
