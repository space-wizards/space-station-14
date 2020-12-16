using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;

namespace Content.Server.Interfaces.Chemistry
{
    /// <summary>
    /// Chemical reaction effect on the world such as an explosion, EMP, or fire.
    /// </summary>
    public interface IReactionEffect : IExposeData
    {
        void React(IEntity solutionEntity, double intensity);
    }
}
