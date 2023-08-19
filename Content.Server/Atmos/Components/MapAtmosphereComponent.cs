using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;

namespace Content.Server.Atmos.Components;

/// <summary>
///     Component that defines the default GasMixture for a map.
/// </summary>
[RegisterComponent, Access(typeof(SharedAtmosphereSystem))]
public sealed class MapAtmosphereComponent : SharedMapAtmosphereComponent
{
    /// <summary>
    ///     The default GasMixture a map will have. Space mixture by default.
    /// </summary>
    [DataField("mixture"), ViewVariables(VVAccess.ReadWrite)]
    public GasMixture? Mixture = GasMixture.SpaceGas;

    /// <summary>
    ///     Whether empty tiles will be considered space or not.
    /// </summary>
    [DataField("space"), ViewVariables(VVAccess.ReadWrite)]
    public bool Space = true;

    /// <summary>
    ///     Whether reactions take place, e.g. plasma fires.
    /// </summary>
    [DataField("simulated"), ViewVariables(VVAccess.ReadWrite)]
    public bool Simulated;

    /// <summary>
    ///     Seconds since last atmos update.
    /// </summary>
    [ViewVariables]
    public float Timer;
}
