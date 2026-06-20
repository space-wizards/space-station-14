using Content.Shared.DeviceNetwork.Events;
using Content.Shared.Interaction;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Components.Devices;
using Content.Shared.DeviceNetwork.Payloads;

namespace Content.Shared.DeviceNetwork.Systems.Devices;

public sealed partial class ApcNetSwitchSystem : EntitySystem
{
    [Dependency] private SharedDeviceNetworkSystem _deviceNetworkSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ApcNetSwitchComponent, InteractHandEvent>(OnInteracted);
        SubscribeLocalEvent<ApcNetSwitchComponent, DeviceNetworkPacketEvent>(OnPackedReceived);
    }

    /// <summary>
    /// Toggles the state of the switch and sends a <see cref="TogglePayload"/>.
    /// </summary>
    private void OnInteracted(Entity<ApcNetSwitchComponent> ent, ref InteractHandEvent args)
    {
        var (uid, component) = ent;
        if (!TryComp(uid, out DeviceNetworkComponent? networkComponent))
            return;

        component.State = !component.State;
        Dirty(ent);

        if (networkComponent.TransmitFrequency == null)
            return;

        var payload = new TogglePayload
        {
            Enabled = component.State,
        };

        _deviceNetworkSystem.QueuePacket(uid, null, payload);
        args.Handled = true;
    }

    /// <summary>
    /// Listens to the <see cref="TogglePayload"/> of other switches to sync state.
    /// </summary>
    private void OnPackedReceived(Entity<ApcNetSwitchComponent> ent, ref DeviceNetworkPacketEvent args)
    {
        var (uid, component) = ent;
        if (!TryComp(uid, out DeviceNetworkComponent? networkComponent)
            || args.SenderAddress == networkComponent.Address)
            return;

        if (args.Data is not TogglePayload toggle)
            return;

        component.State = toggle.Enabled;
        Dirty(ent);
    }
}
