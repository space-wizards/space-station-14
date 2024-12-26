namespace Content.Shared.Light;

public sealed class LightToggleEvent(bool isOn) : EntityEventArgs
{
    public bool IsOn = isOn;
}
