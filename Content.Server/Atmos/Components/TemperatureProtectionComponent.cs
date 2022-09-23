namespace Content.Server.Atmos.Components;

[RegisterComponent]
public sealed class TemperatureProtectionComponent : Component
{
    /// <summary>
    ///     How much to multiply temperature deltas by.
    /// </summary>
    [DataField("coefficient")]
    public float Coefficient = 1.0f;

    /// <summary>
    ///     The examine group used for grouping together examine details.
    /// </summary>
    [DataField("examineGroup")] public string ExamineGroup = "atmos";

    [DataField("examinePriority")] public int ExaminePriority = 1;
}
