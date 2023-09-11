using Content.Shared.Actions;
using Content.Shared.DeviceNetwork.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.DeviceNetwork.Systems;

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
        args.State = new NetworkConfiguratorComponentState(GetNetEntity(comp.ActiveDeviceList), comp.LinkModeActive);
    }

    private void HandleNetworkConfiguratorState(EntityUid uid, NetworkConfiguratorComponent comp,
        ref ComponentHandleState args)
    {
        if (args.Current is not NetworkConfiguratorComponentState state)
        {
            return;
        }

        comp.ActiveDeviceList = EnsureEntity<NetworkConfiguratorComponent>(state.ActiveDeviceList, uid);
        comp.LinkModeActive = state.LinkModeActive;
    }
}

public sealed partial class ClearAllOverlaysEvent : InstantActionEvent
{
}

[Serializable, NetSerializable]
public enum NetworkConfiguratorVisuals
{
    Mode
}

[Serializable, NetSerializable]
public enum NetworkConfiguratorLayers
{
    ModeLight
}
