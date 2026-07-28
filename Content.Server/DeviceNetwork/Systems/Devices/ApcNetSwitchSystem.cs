using Content.Server.DeviceNetwork.Components.Devices;
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
        }

        private void OnInteracted(Entity<ApcNetSwitchComponent> ent, ref InteractHandEvent args)
        {
            var (uid, component) = ent;
            if (!TryComp(uid, out DeviceNetworkComponent? networkComponent))
                return;

            component.State = !component.State;

            if (networkComponent.Data.TransmitFrequency == null)
                return;

            var payload = new ApcNetTogglePayload
            {
                Enabled = component.State,
            };

            _deviceNetworkSystem.QueuePacket(uid, null, ref payload);

            args.Handled = true;
        }

        [SubscribeLocalEvent]
        private void OnPackedReceived(Entity<ApcNetSwitchComponent> ent, ref DeviceNetworkPacketEvent<ApcNetTogglePayload> args)
        {
            var (uid, component) = ent;
            if (!TryComp(uid, out DeviceNetworkComponent? networkComponent)
                || args.SenderAddress == networkComponent.Data.Address)
                return;

            component.State = args.Data.Enabled;
        }
    }
}
