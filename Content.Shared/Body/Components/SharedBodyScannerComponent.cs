using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.Body.Components
{
    public abstract class SharedBodyScannerComponent : Component
    {
    }

    [Serializable, NetSerializable]
    public enum BodyScannerUiKey
    {
        Key
    }

    [Serializable, NetSerializable]
    public sealed class BodyScannerUIState : BoundUserInterfaceState
    {
        public readonly Dictionary<string, BodyPartUiState> BodyParts;

        public BodyScannerUIState(Dictionary<string, BodyPartUiState> bodyParts)
        {
            BodyParts = bodyParts;
        }
    }

    [Serializable, NetSerializable]
    public sealed class BodyPartUiState
    {
        public readonly string Name;
        public readonly FixedPoint2 TotalDamage;
        public readonly List<string> Mechanisms;

        public BodyPartUiState(string name, FixedPoint2 totalDamage, List<string> mechanisms)
        {
            Name = name;
            TotalDamage = totalDamage;
            Mechanisms = mechanisms;
        }
    }
}
