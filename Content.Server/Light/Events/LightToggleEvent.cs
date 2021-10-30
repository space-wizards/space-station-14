using Robust.Shared.GameObjects;

namespace Content.Server.Light.Events
{
    public class LightToggleEvent : EntityEventArgs
    {
        public bool IsOn;

        public LightToggleEvent(bool isOn)
        {
            IsOn = isOn;
        }
    }
}
