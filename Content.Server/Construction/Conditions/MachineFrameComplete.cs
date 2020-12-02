using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Construction;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.Construction.Conditions
{
    /// <summary>
    ///     Checks that the entity has all parts needed in the machine frame component.
    /// </summary>
    [UsedImplicitly]
    public class MachineFrameComplete : IEdgeCondition
    {
        public void ExposeData(ObjectSerializer serializer) { }

        public async Task<bool> Condition(IEntity entity)
        {
            if (entity.Deleted || !entity.TryGetComponent<MachineFrameComponent>(out var machineFrame))
                return false;

            return machineFrame.IsComplete;
        }
    }
}
