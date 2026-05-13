namespace Content.Shared.Light;

public sealed partial class LightToggleEvent(bool isOn) : EntityEventArgs
{
    public bool IsOn = isOn;
}

