using System.Threading.Tasks;
using Content.Shared.Construction;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.Construction.Completions
{
    public class DeleteEntity : IEdgeCompleted
    {
        public void ExposeData(ObjectSerializer serializer)
        {
        }

        public async Task Completed(IEntity entity)
        {
            entity.Delete();
        }
    }
}
