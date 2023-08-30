using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.Systems;

public abstract partial class SharedShuttleSystem
{

}

[Serializable, NetSerializable]
public enum EmergencyConsoleUiKey : byte
{
    Key,
}
