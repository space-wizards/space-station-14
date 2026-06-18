using Content.Server.DeviceNetwork.Components.Devices;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Interaction;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Payloads;

namespace Content.Server.DeviceNetwork.Systems.Devices
{
    public sealed partial class ApcNetSwitchSystem : EntitySystem
    {
        [Dependency] private DeviceNetworkSystem _deviceNetworkSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ApcNetSwitchComponent, InteractHandEvent>(OnInteracted);
            SubscribeLocalEvent<ApcNetSwitchComponent, DeviceNetworkPacketEvent>(OnPackedReceived);
        }

        /// <summary>
        /// Toggles the state of the switch and sents a <see cref="DeviceNetworkConstants.CmdSetState"/> command with the
        /// <see cref="DeviceNetworkConstants.StateEnabled"/> value set to state.
        /// </summary>
        private void OnInteracted(EntityUid uid, ApcNetSwitchComponent component, InteractHandEvent args)
        {
            if (!TryComp(uid, out DeviceNetworkComponent? networkComponent)) return;

            component.State = !component.State;

            if (networkComponent.TransmitFrequency == null)
                return;

            var payload = new TogglePayload
            {
                Enabled = component.State,
            };

            _deviceNetworkSystem.QueuePacket(uid, null, payload, device: networkComponent);

            args.Handled = true;
        }

        /// <summary>
        /// Listens to the <see cref="DeviceNetworkConstants.CmdSetState"/> command of other switches to sync state
        /// </summary>
        private void OnPackedReceived(Entity<ApcNetSwitchComponent> ent, ref DeviceNetworkPacketEvent args)
        {
            var (uid, component) = ent;
            if (!TryComp(uid, out DeviceNetworkComponent? networkComponent) || args.SenderAddress == networkComponent.Address) return;
            if (args.Data is not TogglePayload toggle) return;

            component.State = toggle.Enabled;
        }
    }
}
