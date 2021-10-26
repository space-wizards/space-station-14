using System.Threading.Tasks;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Construction
{
    [ImplicitDataDefinitionForInheritors]
    public interface IGraphAction
    {
        void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager);
    }
}
