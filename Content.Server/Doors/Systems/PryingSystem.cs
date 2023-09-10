using Content.Shared.Doors.Prying.Systems;

namespace Content.Server.Doors.Prying.Systems;

// Why does this exist you may be asking? Because if you call the
// SharedDoorSystem in the server DoorSystem when it attempts to pry open or
// closed a door it will run the open method of the door system twice (once from the
// shared system and once from the server system) duplicating the door opening and closing sound.
public sealed class DoorPryingSystem : SharedDoorPryingSystem
{

}
