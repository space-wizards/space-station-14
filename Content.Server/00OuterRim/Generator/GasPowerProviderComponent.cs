using Content.Server.Atmos;

namespace Content.Server._00OuterRim.Generator;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed class GasPowerProviderComponent : Component
{
    /// <summary>
    /// Past this temperature we assume we're in reaction mass mode and not magic mode.
    /// </summary>
    [DataField("maxTemperature")] public float MaxTemperature = 1000.0f;

    [ViewVariables(VVAccess.ReadOnly)]
    public GasMixture Buffer { get; } = new(100.0f);

    [DataField("plasmaMolesConsumedSec")]
    public float PlasmaMolesConsumedSec = 1.55975875833f / 4;
    [DataField("pressureConsumedSec")]
    public float PressureConsumedSec = 5f;
    [ViewVariables]
    public TimeSpan LastProcess { get; set; } = TimeSpan.Zero;

    public bool Powered = true;
}
