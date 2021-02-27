#nullable enable
using System.Threading.Tasks;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Construction
{
    public interface IEdgeCondition : IExposeData
    {
        Task<bool> Condition(IEntity entity);
        bool DoExamine(IEntity entity, FormattedMessage message, bool inExamineRange) { return false; }
    }
}
