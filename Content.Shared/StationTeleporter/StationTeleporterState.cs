using Content.Shared.StationTeleporter.Components;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.StationTeleporter;

[Serializable, NetSerializable]
public enum StationTeleporterConsoleUIKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class StationTeleporterState(List<StationTeleporterStatus> teleporters, NetEntity? selected = null)
    : BoundUserInterfaceState
{
    public NetEntity? SelectedTeleporter = selected;
    public List<StationTeleporterStatus> Teleporters = teleporters;
}

[Serializable, NetSerializable]
public sealed class StationTeleporterStatus(
    NetEntity teleporterUid,
    NetCoordinates coordinates,
    NetCoordinates? link,
    string name,
    bool powered)
{
    public NetEntity TeleporterUid = teleporterUid;
    public NetCoordinates? Coordinates = coordinates;
    public NetCoordinates? LinkCoordinates = link;
    public string Name = name;
    public bool Powered = powered;
}

/// <summary>
/// Sent when the client clicks on any active teleporter shown by a <see cref="StationTeleporterConsoleComponent"/>.
/// </summary>
[Serializable, NetSerializable]
public sealed class StationTeleporterClickMessage(NetEntity? teleporter) : BoundUserInterfaceMessage
{
    public NetEntity? Teleporter = teleporter;
}

[Serializable, NetSerializable]
public enum TeleporterPortalVisuals
{
    Color,
}
