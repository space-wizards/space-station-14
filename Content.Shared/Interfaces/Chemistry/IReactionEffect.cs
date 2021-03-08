#nullable enable
using Robust.Shared.GameObjects;

namespace Content.Shared.Interfaces.Chemistry
{
    /// <summary>
    /// Chemical reaction effect on the world such as an explosion, EMP, or fire.
    /// </summary>
    public interface IReactionEffect
    {
        void React(IEntity solutionEntity, double intensity);
    }
}
