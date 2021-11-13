using Content.Shared.Chemistry.Components;
using Robust.Shared.GameObjects;

namespace Content.Shared.Chemistry.Reaction
{
    /// <summary>
    /// Chemical reaction effect on the world such as an explosion, EMP, or fire.
    /// </summary>
    public interface IReactionEffect
    {
        void React(Solution solution, EntityUid solutionEntity, double intensity, IEntityManager entityManager);
    }
}
