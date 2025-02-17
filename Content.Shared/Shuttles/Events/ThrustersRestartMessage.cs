using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.Events;

[Serializable, NetSerializable]
public sealed class ThrustersRestartMessage : BoundUserInterfaceMessage
{
    public NetEntity ShuttleEntity;
    public float GyroscopeThrust;
    public float ThrustersThrust;
}
