namespace Content.Server.Radio.Components;

/// <summary>
/// When enabled prevents from sending messages in range
/// </summary>
[RegisterComponent]
public sealed class RadioJammerComponent : Component
{
    [DataField("enabled"), ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled = false;

    [DataField("range"), ViewVariables(VVAccess.ReadWrite)]
    public float Range = 12f;

    /// <summary>
    /// Power usage per second when enabled
    /// </summary>
    [DataField("wattage"), ViewVariables(VVAccess.ReadWrite)]
    public float Wattage = 6f;
}