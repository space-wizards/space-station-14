using Content.Server.Construction.Components;
using Content.Shared.Construction;
using JetBrains.Annotations;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class MachineFrameRegenerateProgress : IGraphAction
    {
        public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
        {
            if (entityManager.TryGetComponent<MachineFrameComponent>(uid, out var machineFrame))
            {
                EntitySystem.Get<MachineFrameSystem>().RegenerateProgress(machineFrame);
            }
        }
    }
}
