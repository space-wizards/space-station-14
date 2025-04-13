using Content.Server.Atmos.Piping.Binary.EntitySystems;
using Content.Shared.Atmos;

namespace Content.Server.Atmos.Piping.Binary.Components;

/// <summary>
/// Component side of the <see cref="GasPressureReliefValveSystem"/>.
/// </summary>
[RegisterComponent]
public sealed partial class GasPressureReliefValveComponent : Component
{
    [DataField]
    public string InletName = "inlet";

    [DataField]
    public string OutletName = "outlet";

    [DataField]
    public float Threshold;

    /// <summary>
    /// Determines whether the valve is open or closed.
    /// Used for showing the valve animation.
    /// </summary>
    [DataField]
    public bool Enabled;

    /// <summary>
    /// The max transfer rate of the valve.
    /// </summary>
    [DataField]
    public float MaxTransferRate = Atmospherics.MaxTransferRate;
}
