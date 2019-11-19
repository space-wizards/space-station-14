using System;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Shared.GameObjects
{
    /// <summary>
    ///     A verb is an action in the right click menu of an entity.
    ///     Global verbs are visible on all entities, regardless of their components.
    /// </summary>
    /// <remarks>
    ///     To add a global verb to all entities,
    ///     define it and mark it with <see cref="GlobalVerbAttribute"/>
    /// </remarks>
    public abstract class GlobalVerb
    {
        /// <summary>
        ///     If true, this verb requires the user to be within
        ///     <see cref="VerbUtility.InteractionRange"/> meters from the entity on which this verb resides.
        /// </summary>
        public virtual bool RequireInteractionRange => true;

        /// <summary>
        ///     Gets the text string that will be shown to <paramref name="user"/> in the right click menu.
        /// </summary>
        /// <param name="user">The entity of the user opening this menu.</param>
        /// <returns>The text string that is shown in the right click menu for this verb.</returns>
        public abstract string GetText(IEntity user, IEntity target);

        /// <summary>
        ///     Gets the visibility level of this verb in the right click menu.
        /// </summary>
        /// <param name="user">The entity of the user opening this menu.</param>
        /// <returns>The visibility level of the verb in the client's right click menu.</returns>
        public abstract VerbVisibility GetVisibility(IEntity user, IEntity target);

        /// <summary>
        ///     Invoked when this verb is activated from the right click menu.
        /// </summary>
        /// <param name="user">The entity of the user opening this menu.</param>
        /// <param name="target">The entity that is being acted upon.</param>
        public abstract void Activate(IEntity user, IEntity target);
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
