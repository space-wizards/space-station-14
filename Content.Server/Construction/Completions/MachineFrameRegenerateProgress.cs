#nullable enable
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Construction;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    public class MachineFrameRegenerateProgress : IGraphAction
    {
        public void ExposeData(ObjectSerializer serializer)
        { }

        public async Task PerformAction(IEntity entity, IEntity? user)
        {
            if (entity.Deleted)
                return;

            if (entity.TryGetComponent<MachineFrameComponent>(out var machineFrame))
            {
                machineFrame.RegenerateProgress();
            }
        }
    }
}
