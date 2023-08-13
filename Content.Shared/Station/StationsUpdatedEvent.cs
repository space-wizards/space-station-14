using Robust.Shared.Serialization;

namespace Content.Shared.Station;

[NetSerializable, Serializable]
public sealed class StationsUpdatedEvent : EntityEventArgs
{
    public readonly HashSet<NetEntity> Stations;

    public StationsUpdatedEvent(HashSet<NetEntity> stations)
    {
        Stations = stations;
    }
}
