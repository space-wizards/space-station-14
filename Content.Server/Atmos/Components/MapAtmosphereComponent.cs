namespace Content.Server.Atmos.Components;

/// <summary>
///     Component that defines the default GasMixture for a map.
/// </summary>
/// <remarks>Honestly, no need to [Friend] this. It's just two simple data fields... Change them to your heart's content.</remarks>
[RegisterComponent]
public sealed class MapAtmosphereComponent : Component
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
}
