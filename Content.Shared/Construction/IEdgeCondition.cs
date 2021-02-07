using System.Threading.Tasks;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Construction
{
    public interface IEdgeCondition : IExposeData
    {
        Task<bool> Condition(IEntity entity);
        bool DoExamine(IEntity entity, FormattedMessage message, bool inExamineRange) { return false; }
    }
}
