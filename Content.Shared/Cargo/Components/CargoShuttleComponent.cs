using Robust.Shared.Map;

namespace Content.Shared.Cargo.Components;

/// <summary>
/// Present on cargo shuttles to provide metadata such as preventing spam calling.
/// </summary>
[RegisterComponent]
public sealed class CargoShuttleComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("nextCall")]
    public TimeSpan? NextCall;

    [ViewVariables(VVAccess.ReadWrite), DataField("cooldown")]
    public float Cooldown = 15f;

    /// <summary>
    /// The shuttle's assigned coordinates on the cargo map.
    /// </summary>
    [ViewVariables]
    public EntityCoordinates Coordinates;

    /// <summary>
    /// The assigned station for this cargo shuttle.
    /// </summary>
    [ViewVariables, DataField("station")]
    public EntityUid? Station;
}
