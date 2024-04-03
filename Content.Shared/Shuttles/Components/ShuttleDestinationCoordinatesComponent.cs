namespace Content.Shared.Shuttles.Components;
using Robust.Shared.GameStates;

/// <summary>
/// Enables a shuttle to travel to a destination with an item inserted into its console
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ShuttleDestinationCoordinatesComponent : Component
{
    /// <summary>
    /// Uid for entity containing the FTLDestination component
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public EntityUid? Destination;
}
