using Content.Shared.Shipyard.Components;

namespace Content.Server.Shipyard.Components;

[RegisterComponent]
[ComponentReference(typeof(SharedShipyardConsoleComponent))]
public sealed class ShipyardConsoleComponent : SharedShipyardConsoleComponent {}
