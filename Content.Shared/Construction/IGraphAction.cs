using System.Threading.Tasks;
using Robust.Shared.GameObjects;

namespace Content.Shared.Construction
{
    public interface IGraphAction
    {
        void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager);
    }
}
