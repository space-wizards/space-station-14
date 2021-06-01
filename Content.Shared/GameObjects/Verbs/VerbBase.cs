#nullable enable

using Robust.Shared.GameObjects;

namespace Content.Shared.GameObjects.Verbs
{
    public abstract class VerbBase
    {
        /// <summary>
        ///     If true, this verb requires the user to be inside within
        ///     <see cref="VerbUtility.InteractionRange"/> meters from the entity on which this verb resides.
        /// </summary>
        public virtual bool RequireInteractionRange => true;

        /// <summary>
        ///     If true, this verb requires both the user and the entity on which
        ///     this verb resides to be in the same container or no container.
        ///     OR the user can be the entity's container
        /// </summary>
        public virtual bool BlockedByContainers => true;

        /// <summary>
        ///     Retrieves the data for this verb.
        ///     This gets redirected to the actual GetData function,
        ///     and therefore should only be overridden in the "base-layer" Verb/GlobalVerb class.
        /// </summary>
        /// <remarks>
        ///     Implementations should write into <paramref name="data"/> to return their data.
        /// </remarks>
        /// <param name="user">The entity of the user opening this menu.</param>
        /// <param name="entity">The target entity for which this verb is being loaded.</param>
        /// <param name="data">The data that must be filled into.</param>
        /// <param name="entry">Additional information such as component.</param>
        public abstract void GetDataFromEntry(IEntity user, IEntity entity, VerbData data, ref VerbEntry entry);

        /// <summary>
        ///     Executes the verb with the given context.
        ///     This gets redirected to the actual Activate function,
        ///     and therefore should only be overridden in the "base-layer" Verb/GlobalVerb class.
        /// </summary>
        public abstract void ActivateFromEntry(IEntity user, IEntity entity, ref VerbEntry entry);

        public VerbData GetDataFromEntry(IEntity user, IEntity entity, ref VerbEntry entry)
        {
            var data = new VerbData();
            GetDataFromEntry(user, entity, data, ref entry);
            return data;
        }
    }

    // Represents a verb with the component address.
    // This is entity-specific (IComponent reference) but does not include all information that's contextually known.
    // The point of this struct is specifically to cover a contextualized Verb while being able to still hold a GlobalVerb.
    public struct VerbEntry
    {
        public IComponent? Component;
        // Verb or GlobalVerb
        public VerbBase Verb;
        // This is the awful mechanism used to address a verb.
        // Right now the largest improvement I can do is centralize it here.
        public string VerbAddress => (Component != null) ? $"{Component!.GetType()}:{Verb.GetType()}" : Verb.GetType().ToString();

        public VerbEntry(IComponent c, Verb v)
        {
            Component = c;
            Verb = v;
        }

        public VerbEntry(GlobalVerb v)
        {
            Component = null;
            Verb = v;
        }
    }
}
