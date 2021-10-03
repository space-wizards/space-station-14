using System.Threading.Tasks;
using Content.Shared.Examine;
using Robust.Shared.GameObjects;

namespace Content.Shared.Construction
{
    public interface IGraphCondition
    {
        Task<bool> Condition(IEntity entity);
        bool DoExamine(ExaminedEvent args) { return false; }
    }
}
