using Robust.Shared.GameObjects;

namespace Content.Server.Power.Components
{
    public class PowerChangedEvent : EntityEventArgs
    {
        //Sends a signal with a Powered bool upon detecting changes in power
        public bool Powered { get; }

        public PowerChangedEvent(bool powered) 
        {
            this.Powered = powered;
        }
    }
}
