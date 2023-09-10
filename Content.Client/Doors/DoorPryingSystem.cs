using Content.Shared.Doors.Prying.Systems;

namespace Content.Client.Doors.Prying.Systems;

// Why does this exist you may be asking? Since the server prying system exists
// for issues listed here, in order for the tool's prying sound to be triggered
// we need this.
public sealed class DoorPryingSystem : SharedDoorPryingSystem
{ }
