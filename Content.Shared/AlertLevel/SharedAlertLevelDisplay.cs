using Robust.Shared.Serialization;

namespace Content.Shared.AlertLevel;

[Serializable, NetSerializable]
public enum AlertLevelDisplay
{
    CurrentLevel,
    Layer
}
