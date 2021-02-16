#nullable enable
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

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
