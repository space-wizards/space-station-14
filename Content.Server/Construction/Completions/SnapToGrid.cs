#nullable enable
using System.Threading.Tasks;
using Content.Server.Utility;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Maths;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    public class SnapToGrid : IGraphAction
    {
        public SnapGridOffset Offset { get; private set; } = SnapGridOffset.Center;
        public bool SouthRotation { get; private set; } = false;

        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.Offset, "offset", SnapGridOffset.Center);
            serializer.DataField(this, x => x.SouthRotation, "southRotation", false);
        }

        public async Task PerformAction(IEntity entity, IEntity? user)
        {
            if (entity.Deleted) return;

            entity.SnapToGrid(Offset);
            if (SouthRotation)
            {
                entity.Transform.LocalRotation = Angle.Zero;
            }
        }
    }
}
