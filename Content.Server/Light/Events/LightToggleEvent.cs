namespace Content.Server.Light.Events
{
    public sealed class LightToggleEvent : EntityEventArgs
    {
        public bool IsOn;

        public LightToggleEvent(bool isOn)
        {
            IsOn = isOn;
        }
    }
}
