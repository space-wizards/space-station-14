using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Components.Devices;
using Content.Shared.Interaction;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.DeviceNetwork.Systems.Devices
{
    public sealed class ApcNetSwitchSystem : EntitySystem
    {
        [Dependency] private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ApcNetSwitchComponent, InteractHandEvent>(OnInteracted);
            SubscribeLocalEvent<ApcNetSwitchComponent, PacketSentEvent>(OnPackedReceived);
        }

        private void OnInteracted(EntityUid uid, ApcNetSwitchComponent component, InteractHandEvent args)
        {
            if (!EntityManager.TryGetComponent(uid, out DeviceNetworkComponent? networkComponent)) return;

            component.State = !component.State;

            var payload = new NetworkPayload
            {
                [DeviceNetworkConstants.Command] = DeviceNetworkConstants.CmdSetState,
                [DeviceNetworkConstants.StateEnabled] = component.State,
            };

            _deviceNetworkSystem.QueuePacket(uid, DeviceNetworkConstants.NullAddress, networkComponent.Frequency, payload, true);

            args.Handled = true;
        }

        private void OnPackedReceived(EntityUid uid, ApcNetSwitchComponent component, PacketSentEvent args)
        {
            if (!EntityManager.TryGetComponent(uid, out DeviceNetworkComponent? networkComponent) || args.SenderAddress == networkComponent.Address) return;
            if (!args.Data.TryGetValue(DeviceNetworkConstants.Command, out string? command) || command != DeviceNetworkConstants.CmdSetState) return;
            if (!args.Data.TryGetValue(DeviceNetworkConstants.StateEnabled, out bool enabled)) return;

            component.State = enabled;
        }
    }
}
