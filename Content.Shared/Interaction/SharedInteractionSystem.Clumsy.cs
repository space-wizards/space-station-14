using Content.Shared.Interaction.Components;
using Robust.Shared.Random;

namespace Content.Shared.Interaction
{
    public partial class SharedInteractionSystem
    {
        public bool RollClumsy(ClumsyComponent component, float chance)
        {
            return component.Running && _random.Prob(chance);
        }

        /// <summary>
        ///     Rolls a probability chance for a "bad action" if the target entity is clumsy.
        /// </summary>
        /// <param name="entity">The entity that the clumsy check is happening for.</param>
        /// <param name="chance">
        /// The chance that a "bad action" happens if the user is clumsy, between 0 and 1 inclusive.
        /// </param>
        /// <returns>True if a "bad action" happened, false if the normal action should happen.</returns>
        public bool TryRollClumsy(EntityUid entity, float chance, ClumsyComponent? component = null)
        {
            return Resolve(entity, ref component, false) && RollClumsy(component, chance);
        }
    }
}
