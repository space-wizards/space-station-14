using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.BUIStates;

[Serializable, NetSerializable]
public sealed class EmergencyShuttleConsoleBoundUserInterfaceState : BoundUserInterfaceState
{
    public List<string> Authorizations = new();
    public int AuthorizationsRequired;

    public TimeSpan? TimeToLaunch;
}
