using Robust.Shared.Serialization;

namespace Content.Shared.Screen;

[Serializable, NetSerializable]
public enum ScreenType : byte
{
    Text,
    ShuttleTime,
    AlertLevel
}
