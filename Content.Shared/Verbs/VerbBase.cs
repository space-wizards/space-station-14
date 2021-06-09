#nullable enable

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
    }
}
