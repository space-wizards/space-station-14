using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.DeviceNetwork;

public abstract class SharedNetworkConfiguratorSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NetworkConfiguratorComponent, ComponentGetState>(GetNetworkConfiguratorState);
        SubscribeLocalEvent<NetworkConfiguratorComponent, ComponentHandleState>(HandleNetworkConfiguratorState);
    }

    private void GetNetworkConfiguratorState(EntityUid uid, NetworkConfiguratorComponent comp,
        ref ComponentGetState args)
    {
        args.State = new NetworkConfiguratorComponentState(comp.ActiveDeviceList);
    }

    private void HandleNetworkConfiguratorState(EntityUid uid, NetworkConfiguratorComponent comp,
        ref ComponentHandleState args)
    {
        if (args.Current is not NetworkConfiguratorComponentState state)
        {
            return;
        }

        comp.ActiveDeviceList = state.ActiveDeviceList;
    }
}

public sealed class ClearAllOverlaysEvent : InstantActionEvent
{
}
