using Robust.Shared.Serialization;
using Content.Shared.Chemistry.Components;

namespace Content.Shared.MedicalScanner
{
    public abstract class SharedMedicalResearchBedComponent : Component
    {
        /// <summary>
        ///     On interacting with an entity retrieves the entity UID for use with getting the current damage of the mob.
        /// </summary>
        [Serializable, NetSerializable]
        public sealed class MedicalResearchBedScannedUserMessage : BoundUserInterfaceMessage
        {
            public readonly EntityUid? BuckledEntity;
            public readonly IReadOnlyList<Solution.ReagentQuantity> BufferReagents;

            public MedicalResearchBedScannedUserMessage(EntityUid? buckledEntity, IReadOnlyList<Solution.ReagentQuantity> bufferReagents)
            {
                BuckledEntity = buckledEntity;
                BufferReagents = bufferReagents;
            }
        }

        [Serializable, NetSerializable]
        public enum MedicalResearchBedUiKey : byte
        {
            Key
        }
    }
}
