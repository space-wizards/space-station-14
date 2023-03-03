namespace Content.Server.Radio.Components;

/// <summary>
/// When activated prevents from sending messages in range
/// </summary>
[RegisterComponent]
public sealed class RadioJammerComponent : Component
{
    [DataField("activated"), ViewVariables(VVAccess.ReadWrite)]
    public bool Activated = false;

    [DataField("range"), ViewVariables(VVAccess.ReadWrite)]
    public float Range = 8f;

    /// <summary>
    /// Power usage per second when enabled
    /// </summary>
    [DataField("wattage"), ViewVariables(VVAccess.ReadWrite)]
    public float Wattage = 6f;
}