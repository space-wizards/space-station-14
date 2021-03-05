#nullable enable
using System.Threading.Tasks;
using Robust.Shared.GameObjects;

namespace Content.Shared.Construction
{
    public interface IGraphAction
    {
        Task PerformAction(IEntity entity, IEntity? user);
    }
}
