using System.Threading.Tasks;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;

namespace Content.Shared.Construction
{
    public interface IEdgeCondition : IExposeData
    {
        Task<bool> Condition(IEntity entity);
    }
}
