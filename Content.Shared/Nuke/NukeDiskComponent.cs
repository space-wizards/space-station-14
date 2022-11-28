using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared.Nuke;

/// <summary>
/// Used for tracking and respawning the nuke disk - isn't a tag for pinpointer purposes.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class NukeDiskComponent : Component
{
    [ViewVariables]
    [DataField("station")]
    public EntityUid Station = EntityUid.Invalid;

    [ViewVariables]
    [DataField("stationMap")]
    public (EntityUid?, EntityUid?) StationMap;

    /// <summary>
    /// Checks if the disk should respawn on the station grid
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("respawn")]
    public bool Respawn = true;

    public string Disk = "NukeDisk";
}

public sealed class NukeDiskStationSetupEvent : EntityEventArgs
{
    public EntityUid Disk;
    public NukeDiskComponent DiskComp;

    public NukeDiskStationSetupEvent(EntityUid disk,NukeDiskComponent diskComp)
    {
        DiskComp = diskComp;
        Disk = disk;
    }
}
