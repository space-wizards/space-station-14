using Content.Shared.Shipyard;

namespace Content.Shared.Shipyard.Components;

/// <summary>
/// Tied to an ID card when a ship is purchased. 1 ship per captain.
/// </summary>
[RegisterComponent, Access(typeof(SharedShipyardSystem))]
public sealed class ShuttleDeedComponent : Component
{
    [DataField("shuttleuid")]
    public EntityUid? ShuttleUid;

    [DataField("shuttlename")]
    public string? ShuttleName;
}
