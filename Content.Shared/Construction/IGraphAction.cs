#nullable enable
using System.Threading.Tasks;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.Construction
{
    public interface IGraphAction : IExposeData
    {
        Task PerformAction(IEntity entity, IEntity? user);
    }
}
