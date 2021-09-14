using System;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Shared.Verbs
{
    /// <summary>
    ///     A verb is an action in the right click menu of an entity.
    ///     Global verbs are visible on all entities, regardless of their components.
    /// </summary>
    /// <remarks>
    ///     To add a global verb to all entities,
    ///     define it and mark it with <see cref="GlobalVerbAttribute"/>
    /// </remarks>
    public abstract class GlobalVerb : VerbBase
    {
        /// <summary>
        ///     Gets the visible verb data for the user.
        /// </summary>
        /// <remarks>
        ///     Implementations should write into <paramref name="data"/> to return their data.
        /// </remarks>
        /// <param name="user">The entity of the user opening this menu.</param>
        /// <param name="target">The entity this verb is being evaluated for.</param>
        /// <param name="data">The data that must be filled in.</param>
        /// <returns>The text string that is shown in the right click menu for this verb.</returns>
        public abstract void GetData(IEntity user, IEntity target, VerbData data);

        /// <summary>
        ///     Invoked when this verb is activated from the right click menu.
        /// </summary>
        /// <param name="user">The entity of the user opening this menu.</param>
        /// <param name="target">The entity that is being acted upon.</param>
        public abstract void Activate(IEntity user, IEntity target);

        public VerbData GetData(IEntity user, IEntity target)
        {
            var data = new VerbData();
            GetData(user, target, data);
            return data;
        }
    }

    /// <summary>
    /// This attribute should be used on <see cref="GlobalVerb"/>. These are verbs which are on visible for all entities,
    /// regardless of the components they contain.
    /// </summary>
    [MeansImplicitUse]
    [BaseTypeRequired(typeof(GlobalVerb))]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class GlobalVerbAttribute : Attribute
    {
    }
}
