using Content.Server.Radio.EntitySystems;

namespace Content.Server.Radio.Components;

/// <summary>
/// When activated (<see cref="ActiveRadioJammerComponent"/>) prevents from sending messages in range
/// </summary>
[RegisterComponent]
[Access(typeof(JammerSystem))]
public sealed partial class RadioJammerComponent : Component
{
    /// <summary>
    /// Range of the jammer when on high power
    /// </summary>
    [DataField("highPowerRange"), ViewVariables(VVAccess.ReadOnly)]
    public float HighPowerRange = 12f;

    /// <summary>
    /// Range of the jammer when on medium power
    /// </summary>
    [DataField("mediumPowerRange"), ViewVariables(VVAccess.ReadOnly)]
    public float MediumPowerRange = 6f;

    /// <summary>
    /// Range of the jammer when on low power
    /// </summary>
    [DataField("lowPowerRange"), ViewVariables(VVAccess.ReadOnly)]
    public float LowPowerRange = 2.5f;

    /// <summary>
    /// Power usage per second when on high power
    /// </summary>
    [DataField("highPowerWattage"), ViewVariables(VVAccess.ReadOnly)]
    public float HighPowerWattage = 12f;

    /// <summary>
    /// Power usage per second when on medium power
    /// </summary>
    [DataField("mediumPowerWattage"), ViewVariables(VVAccess.ReadOnly)]
    public float MediumPowerWattage = 2f;

    /// <summary>
    /// Power usage per second when on low power
    /// </summary>
    [DataField("lowPowerWattage"), ViewVariables(VVAccess.ReadOnly)]
    public float LowPowerWattage = 1f;

    /// <summary>
    /// The currently selected power level
    /// </summary>
    [DataField("selectedPowerLevel"), ViewVariables(VVAccess.ReadWrite)]
    public byte SelectedPowerLevel = 1;

}
