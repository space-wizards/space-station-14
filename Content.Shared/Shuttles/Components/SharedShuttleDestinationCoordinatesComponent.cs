namespace Content.Shared.Shuttles.Components;
using Robust.Shared.GameStates;

/// <summary>
/// Enables a shuttle to travel to a destination with an item inserted into its console
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SharedShuttleDestinationCoordinatesComponent : Component
{
    // Uid for entity containing the FTLDestination component
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public EntityUid? Destination;
}
