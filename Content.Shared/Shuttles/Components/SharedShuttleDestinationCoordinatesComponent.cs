namespace Content.Shared.Shuttles.Components;

/// <summary>
/// Enables a shuttle to travel to a destination with an item inserted into its console
/// </summary>
[RegisterComponent]
public sealed partial class SharedShuttleDestinationCoordinatesComponent : Component
{
    // Uid for entity containing the FTLDestination component
    [DataField]
    public EntityUid? Destination;
}
