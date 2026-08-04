using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Interaction;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Components.Devices;
using Content.Shared.DeviceNetwork.Payloads;

namespace Content.Shared.DeviceNetwork.Systems.Devices;

public sealed partial class ApcNetSwitchSystem : EntitySystem
{
    [Dependency] private DeviceNetworkSystem _deviceNetworkSystem = default!;

    [Dependency] private EntityQuery<DeviceNetworkComponent> _query = default!;

    /// <summary>
    /// Toggles the state of the switch and sends a <see cref="ApcNetTogglePayload"/>.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnInteracted(Entity<ApcNetSwitchComponent> ent, ref InteractHandEvent args)
    {
        var (uid, component) = ent;
        if (!_query.TryComp(uid, out var networkComponent))
            return;

        component.State = !component.State;
        Dirty(ent);

        if (networkComponent.TransmitFrequency == null)
            return;

        var payload = new ApcNetTogglePayload
        {
            Enabled = component.State,
        };

        _deviceNetworkSystem.SendPacket(uid, null, ref payload);
        args.Handled = true;
    }

    /// <summary>
    /// Listens to the <see cref="ApcNetTogglePayload"/> of other switches to sync state.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnPackedReceived(Entity<ApcNetSwitchComponent> ent, ref DeviceNetworkPacketEvent<ApcNetTogglePayload> args)
    {
        var (uid, component) = ent;
        if (!_query.TryComp(uid, out var networkComponent)
            || args.SenderAddress == networkComponent.Data.AddressId)
            return;

        component.State = args.Data.Enabled;
        Dirty(ent);
    }
}
