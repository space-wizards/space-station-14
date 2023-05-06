using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.MedicalScanner
{
    public abstract class SharedHealthAnalyzerComponent : Component
    {
        /// <summary>
        ///     On interacting with an entity retrieves the entity UID for use with getting the current damage of the mob.
        /// </summary>
        [Serializable, NetSerializable]
        public sealed class HealthAnalyzerScannedUserMessage : BoundUserInterfaceMessage
        {
            public readonly EntityUid? TargetEntity;

            public HealthAnalyzerScannedUserMessage(EntityUid? targetEntity)
            {
                TargetEntity = targetEntity;
            }
        }

        [Serializable, NetSerializable]
        public enum HealthAnalyzerUiKey : byte
        {
            Key
        }
    }

    [Serializable, NetSerializable]
    public sealed class HealthAnalyzerDoAfterEvent : SimpleDoAfterEvent
    {
    }
}
