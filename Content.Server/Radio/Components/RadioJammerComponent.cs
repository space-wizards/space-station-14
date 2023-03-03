namespace Content.Server.Radio.Components;

/// <summary>
/// When enabled prevents from sending messages in range
/// </summary>
[RegisterComponent]
public sealed class RadioJammerComponent : Component
{
    [DataField("enabled"), ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled;
    [DataField("range", required: true), ViewVariables(VVAccess.ReadWrite)]
    public float Range;
}