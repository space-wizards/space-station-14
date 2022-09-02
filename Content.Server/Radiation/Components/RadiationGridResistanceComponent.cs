using Content.Server.Radiation.Systems;
using Content.Shared.Radiation.Components;

namespace Content.Server.Radiation.Components;

/// <summary>
///     Grid component that stores radiation resistance of <see cref="RadiationBlockerComponent"/> per tile.
/// </summary>
[RegisterComponent]
[Access(typeof(RadiationSystem), Other = AccessPermissions.ReadExecute)]
public sealed class RadiationGridResistanceComponent : Component
{
    /// <summary>
    ///     Radiation resistance per tile.
    /// </summary>
    public readonly Dictionary<Vector2i, float> ResistancePerTile = new();
}
