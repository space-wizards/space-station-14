using Robust.Shared.Serialization;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;


namespace Content.Shared.HealthAnalyzer
{
    public abstract class SharedHealthAnalyzerComponent : Component
    {
        [Serializable, NetSerializable]
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
