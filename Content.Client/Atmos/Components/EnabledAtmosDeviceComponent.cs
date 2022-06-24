namespace Content.Client.Atmos.Components;


public abstract class EnabledAtmosDeviceComponent : Component
{
    [DataField("disabledState")]
    public string DisabledState = string.Empty;

    [DataField("enabledState")]
    public string EnabledState = string.Empty;
}
