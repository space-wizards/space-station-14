using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Components.Devices;
using Content.Shared.Interaction;

namespace Content.Server.DeviceNetwork.Systems.Devices;

/// <summary>
///     This system handles sending a single enum to a set of devices when the
///     entity in question has been interacted with.
/// </summary>
public sealed class DeviceNetworkedButtonSystem : EntitySystem
{
    [Dependency] private readonly DeviceNetworkSystem _deviceNetwork = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<DeviceNetworkedButtonComponent, InteractHandEvent>(OnInteractHand);
    }

    private void OnInteractHand(EntityUid uid, DeviceNetworkedButtonComponent component, InteractHandEvent args)
    {
        if (component.SendOnPressed == null)
        {
            return;
        }

        var packet = new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = DeviceNetworkConstants.CmdSetState,
            [DeviceNetworkConstants.CmdSetState] = component.SendOnPressed
        };

        _deviceNetwork.QueuePacket(uid, null, packet);
    }
}
