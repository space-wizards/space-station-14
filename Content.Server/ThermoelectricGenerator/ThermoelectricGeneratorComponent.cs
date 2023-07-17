namespace Content.Server.ThermoelectricGenerator;

[RegisterComponent]
public sealed class ThermoelectricGeneratorComponent : Component
{
    /// <summary>
    /// Name of the cold loop
    /// </summary>
    [DataField("coldLoopName")] public string ColdLoopName = "cold";
    /// <summary>
    /// Name of the hotloop
    /// </summary>
    [DataField("hotLoopName")] public string HotLoopName = "hot";
    /// <summary>
    /// Lerp multiply value
    /// </summary>
    [DataField("transferRate")]public float TransferRate = 0.5f;
    /// <summary>
    /// Power multiplier
    /// </summary>
    [DataField("powerOutput")] public float PowerOutput = 1f;
}
