using Robust.Shared.Serialization;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;

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
            public readonly FixedPoint2 HealthChanges;

            public MedicalResearchBedScannedUserMessage(EntityUid? buckledEntity, IReadOnlyList<Solution.ReagentQuantity> bufferReagents, FixedPoint2 healthChanges)
            {
                BuckledEntity = buckledEntity;
                BufferReagents = bufferReagents;
                HealthChanges = healthChanges;
            }
        }

        [Serializable, NetSerializable]
        public enum MedicalResearchBedUiKey : byte
        {
            Key
        }
    }
}
