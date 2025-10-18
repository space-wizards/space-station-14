﻿using Content.Shared.Atmos;

namespace Content.Server.Power.Generation.Teg;

/// <summary>
/// A "circulator" for the thermo-electric generator (TEG).
/// Circulators are used by the TEG to take in a side of either hot or cold gas.
/// </summary>
/// <seealso cref="TegSystem"/>
[RegisterComponent]
[Access(typeof(TegSystem))]
public sealed partial class TegCirculatorComponent : Component
{
    /// <summary>
    /// The difference between the inlet and outlet pressure at the start of the previous tick.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("lastPressureDelta")]
    public float LastPressureDelta;

    /// <summary>
    /// The amount of moles transferred by the circulator last tick.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("lastMolesTransferred")]
    public float LastMolesTransferred;

    /// <summary>
    /// Minimum pressure delta between inlet and outlet for which the circulator animation speed is "fast".
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("visualSpeedDelta")]
    public float VisualSpeedDelta = 5 * Atmospherics.OneAtmosphere;

    /// <summary>
    /// Light color of this circulator when it's running at "slow" speed.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("lightColorSlow")]
    public Color LightColorSlow = Color.FromHex("#FF3300");

    /// <summary>
    /// Light color of this circulator when it's running at "fast" speed.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("lightColorFast")]
    public Color LightColorFast = Color.FromHex("#AA00FF");

    /// <summary>
    /// Heat capacity of this circulator.
    /// </summary>
    [DataField]
    public float HeatCapacity = 1000f;

    /// <summary>
    /// The conductivity constant of this circulator.
    /// Describes how well the heat flows from the gas to the circulator.
    /// </summary>
    [DataField]
    public float ConductivityConstant = 100f;

    /// <summary>
    /// Current temperature of the circulator.
    /// </summary>
    /// <remarks>That block of copper in there has been sitting for a while, okay?</remarks>
    [DataField]
    public float CirculatorTemperature = Atmospherics.T20C;
}
