using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.Systems;

public abstract class SharedShuttleSystem : EntitySystem
{

}

[Serializable, NetSerializable]
public enum EmergencyShuttleConsoleUiKey : byte
{
    Key,
}
