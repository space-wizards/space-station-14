using System.Threading.Tasks;
using Content.Server.Utility;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    public class SnapToGrid : IEdgeCompleted, IStepCompleted
    {
        public SnapGridOffset Offset { get; private set; }

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.Offset, "offset", SnapGridOffset.Center);
        }

        public async Task StepCompleted(IEntity entity)
        {
            await Completed(entity);
        }

        public async Task Completed(IEntity entity)
        {
            if (entity.Deleted) return;

            entity.SnapToGrid(Offset);
        }
    }
}
