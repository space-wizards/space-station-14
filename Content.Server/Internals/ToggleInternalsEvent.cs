namespace Content.Server.Internals
{
    public class ToggleInternalsEvent
    {
        public bool? ForcedState;
        public ToggleInternalsEvent(bool? force = null)
        {
            ForcedState = force;
        }
    }
}
