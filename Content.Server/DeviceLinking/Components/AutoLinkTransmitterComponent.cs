namespace Content.Server.DeviceLinking.Components;

/// <summary>
/// This is used for automatic linkage with various receivers, like shutters.
/// </summary>
[RegisterComponent]
public sealed partial class AutoLinkTransmitterComponent : Component
{
    [DataField("channel", required: true)]
    public string AutoLinkChannel = default!;
}

