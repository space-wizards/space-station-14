using Robust.Shared.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Maths;
using Content.Server.WireHacking;
using Content.Server.VendingMachines;
using Content.Shared.Sound;
using Content.Shared.SecurityCamera;
using static Content.Shared.Wires.SharedWiresComponent;
using static Content.Shared.Wires.SharedWiresComponent.WiresAction;

namespace Content.Server.SecurityCamera
{
    [RegisterComponent]
    public class SecurityCameraComponent : Component, IWires
    {
        public SoundSpecifier alertsound = new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");

        public bool Connected = true;
        public bool Alert = true;
        public readonly WiresComponent? WiresComponent = null;

        private bool _powerWiresPulsed;
        private bool PowerWiresPulsed
        {
            get => _powerWiresPulsed;
            set
            {
                _powerWiresPulsed = value;
                UpdateWiresStatus();
            }
        }
        public override string Name => "SecurityCamera";

        public void UpdateWiresStatus()
        {
            var connectionlight = new StatusLightData(Color.Yellow, StatusLightState.On, "CONC");
            if (PowerWiresPulsed)
            {
                connectionlight = new StatusLightData(Color.Yellow, StatusLightState.BlinkingFast, "CONC");
            }
            
            {
                connectionlight = new StatusLightData(Color.Red, StatusLightState.On, "CONC");
            }
            if (WiresComponent == null)
            {
                return;
            }
        }
        
        private enum Wires
        {
            /// <summary>
            /// Cutting removes connection permanently.
            /// Mending restores connection.
            /// </summary>
            Connection,

            /// <summary>
            /// Cutting activates a sound alert.
            /// Mending resets the alert.
            /// </summary>
            Fake,
        }

        public void RegisterWires(WiresComponent.WiresBuilder builder)
        {
            builder.CreateWire(Wires.Fake);
            builder.CreateWire(Wires.Connection);

            UpdateWiresStatus();
        }
        
        public void WiresUpdate(WiresUpdateEventArgs args)
        {
            if (args.Action == Mend)
            {
                switch (args.Identifier)
                {
                    case Wires.Connection:
                        ChangeConnected(true);
                        break;
                    case Wires.Fake:
                        Alert = true;
                        break;
                }
            }

            else if (args.Action == Cut)
            {
                switch (args.Identifier)
                {
                    case Wires.Connection:
                        ChangeConnected(false);
                        break;
                    case Wires.Fake:
                        AlertSound();
                        break;
                }
            }

            UpdateWiresStatus();
            //UpdatePowerCutStatus();
        }

        private void AlertSound()
        {
            if(Alert)
            {
                SoundSystem.Play(Filter.Broadcast(), alertsound.GetSound());
                Alert = false;
            }
        }

        private void ChangeConnected(bool connected)
        {
            Connected = connected;
            Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid,new SecurityCameraConnectionChangedEvent(Owner.Uid,connected));
        }
    }
}