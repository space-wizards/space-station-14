using System;
using System.Threading.Tasks;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;

namespace Content.Shared.Construction
{
    public interface IEdgeCompleted : IExposeData
    {
        Task Completed(IEntity entity, IEntity user);
    }
}
