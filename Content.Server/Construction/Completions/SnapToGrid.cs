#nullable enable
using System.Threading.Tasks;
using Content.Server.Utility;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;
using YamlDotNet.Serialization;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    [DataDefinition]
    public class SnapToGrid : IGraphAction
    {
        [DataField("offset")] public SnapGridOffset Offset { get; private set; } = SnapGridOffset.Center;
        [DataField("southRotation")] public bool SouthRotation { get; private set; } = false;

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
