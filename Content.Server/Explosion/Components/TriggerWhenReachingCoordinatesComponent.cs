using System.Numerics;
using Robust.Shared.Map;

namespace Content.Server.Explosion.Components;

/// <summary>
/// Triggers a projectile when they reach the coordinates the shooter was aiming at.
/// </summary>
[RegisterComponent]
public sealed partial class TriggerWhenReachingCoordinatesComponent: Component
{
    /// <summary>
    ///     Coordinates the projectile will trigger at.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("destination")]
    public Vector2 Destination;


    /// <summary>
    ///    Coordinates the projectile was launched from.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("origin")]
    public MapCoordinates? Origin;
}
