namespace Content.Server.MachineLinking.Components;

/// <summary>
/// This is used for automatic linkage with various receivers, like shutters.
/// </summary>
[RegisterComponent]
public sealed partial class AutoLinkTransmitterComponent : Component
{
    [DataField("channel", required: true, readOnly: true)]
    public string AutoLinkChannel = default!;
}

