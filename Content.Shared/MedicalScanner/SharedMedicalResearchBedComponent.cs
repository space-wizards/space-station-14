using Robust.Shared.Serialization;

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

            public MedicalResearchBedScannedUserMessage(EntityUid? buckledEntity)
            {
                BuckledEntity = buckledEntity;
            }
        }

        [Serializable, NetSerializable]
        public enum MedicalResearchBedUiKey : byte
        {
            Key
        }
    }
}
