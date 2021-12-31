using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Random;

namespace Content.Server.Interaction.Components
{
    /// <summary>
    /// A simple clumsy tag-component.
    /// </summary>
    [RegisterComponent]
    public class ClumsyComponent : Component
    {
        [Dependency] private readonly IRobustRandom _random = default!;

        public bool RollClumsy(float chance)
        {
            return Running && _random.Prob(chance);
        }

        /// <summary>
        ///     Rolls a probability chance for a "bad action" if the target entity is clumsy.
        /// </summary>
        /// <param name="entity">The entity that the clumsy check is happening for.</param>
        /// <param name="chance">
        /// The chance that a "bad action" happens if the user is clumsy, between 0 and 1 inclusive.
        /// </param>
        /// <returns>True if a "bad action" happened, false if the normal action should happen.</returns>
        public static bool TryRollClumsy(EntityUid entity, float chance)
        {
            return IoCManager.Resolve<IEntityManager>().TryGetComponent(entity, out ClumsyComponent? clumsy)
                   && clumsy.RollClumsy(chance);
        }
    }
}
