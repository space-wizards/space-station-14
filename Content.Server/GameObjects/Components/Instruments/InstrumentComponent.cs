using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Instruments;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Log;

namespace Content.Server.GameObjects.Components.Instruments
{
    [RegisterComponent]
    public class InstrumentComponent : SharedInstrumentComponent, IDropped, IHandSelected, IHandDeselected
    {
        private INetChannel _instrumentPlayer;

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null, IComponent component = null)
        {
            base.HandleMessage(message, netChannel, component);
            if (netChannel != _instrumentPlayer) return;
            switch (message)
            {
                case InstrumentMidiEventMessage midiEventMsg:
                    SendNetworkMessage(midiEventMsg);
                    break;
            }
        }

        public void Dropped(DroppedEventArgs eventArgs)
        {
            // TODO
        }

        public void HandSelected(HandSelectedEventArgs eventArgs)
        {
            Logger.Info("yay selected");
        }

        public void HandDeselected(HandDeselectedEventArgs eventArgs)
        {
            Logger.Info("NO DESELECTED AAAAAA");
        }
    }
}
