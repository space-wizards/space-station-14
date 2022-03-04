using Robust.Shared.Serialization;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;


namespace Content.Shared.HealthAnalyzer
{
    public abstract class SharedHealthAnalyzerComponent : Component
    {
        [Serializable, NetSerializable]
        /// <summary>
        ///     On interacting with an entity retrieves the entity UID for use with getting the current damage of the mob.
        /// </summary>
        public sealed class HealthAnalyzerScannedUserMessage : BoundUserInterfaceMessage
        {
            public readonly EntityUid? TargetEntity;

            public HealthAnalyzerScannedUserMessage(
                EntityUid? targetEntity)
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
}
