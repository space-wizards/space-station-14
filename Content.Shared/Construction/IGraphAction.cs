using System.Threading.Tasks;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Construction
{
    [ImplicitDataDefinitionForInheritors]
    public interface IGraphAction
    {
        Task PerformAction(IEntity entity, IEntity? user);
    }
}
