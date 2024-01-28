
namespace Content.Server.Shuttle.Components;

/// <summary>
/// Enables a shuttle/pod to travel to a destination with an item inserted
/// </summary>
[RegisterComponent]
public sealed partial class ShuttleDestinationCoordinatesComponent : Component
{
    //This component should be able to return a destination EntityUid based on the whitelist datafield.
    //Right now that functionality is not implemented, defaulting to the Central Command map as only one item (CentCom Coords Disk) uses this component.

    [DataField]
    public string Destination = "Central Command";
}
